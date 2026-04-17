using FYP_App.Controllers;
using FYP_App.Data;
using FYP_App.Models;
using FYP_App.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace FYP_App.Tests.Controllers
{
    [TestFixture]
    public class PanelControllerTests
    {
        private ApplicationDbContext _context;
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private PanelController _controller;
        private string _currentUserId;

        [SetUp]
        public void Setup()
        {
            _currentUserId = Guid.NewGuid().ToString();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "PanelTestDb_" + Guid.NewGuid())
                .Options;
            _context = new ApplicationDbContext(options);

            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            _userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_currentUserId);

            _controller = new PanelController(_userManagerMock.Object, _context);

            // Setup HttpContext with User
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _currentUserId)
            }));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Initialize TempData
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Database?.EnsureDeleted();
            _context?.Dispose();
        }

        #region Dashboard Tests

        [Test]
        public async Task Index_ShouldReturnDefensesForPanelMembersPanel()
        {
            // Arrange
            var panel = TestDataFactory.CreatePanel(1, "Test Panel", true);
            await _context.Panels.AddAsync(panel);

            var panelMember = new PanelMember
            {
                PanelId = 1,
                UserId = _currentUserId,
                Role = "Internal"
            };
            await _context.PanelMembers.AddAsync(panelMember);

            var project = TestDataFactory.CreateProject(1);
            await _context.Projects.AddAsync(project);

            var defense = new DefenseSchedule
            {
                Id = 1,
                ProjectId = 1,
                PanelId = 1,
                DefenseType = "Final Defense",
                Date = DateTime.Now.AddDays(7),
                Room = "Room 101"
            };
            await _context.DefenseSchedules.AddAsync(defense);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var model = result.Model as List<DefenseSchedule>;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task Index_WhenNotAssignedToPanel_ShouldReturnEmptyList()
        {
            // Arrange
            var panel = TestDataFactory.CreatePanel(1, "Other Panel", true);
            await _context.Panels.AddAsync(panel);

            var otherMember = new PanelMember
            {
                PanelId = 1,
                UserId = "other-user-id",
                Role = "Internal"
            };
            await _context.PanelMembers.AddAsync(otherMember);

            var defense = new DefenseSchedule
            {
                Id = 1,
                ProjectId = 1,
                PanelId = 1,
                DefenseType = "Final Defense",
                Date = DateTime.Now.AddDays(7),
                Room = "Room 101"
            };
            await _context.DefenseSchedules.AddAsync(defense);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var model = result.Model as List<DefenseSchedule>;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Count, Is.EqualTo(0));
        }

        #endregion

        #region Evaluate Tests

        [Test]
        public async Task Evaluate_Get_ValidProject_ShouldReturnView()
        {
            // Arrange
            var project = TestDataFactory.CreateProject(1);
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Evaluate(project.Id, "Final Defense") as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var model = result.Model as Project;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Id, Is.EqualTo(project.Id));
            Assert.That(_controller.ViewBag.DefenseType, Is.EqualTo("Final Defense"));
        }

        [Test]
        public async Task Evaluate_Get_NonExistentProject_ShouldReturnNotFound()
        {
            // Act
            var result = await _controller.Evaluate(999, "Final Defense");

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        #endregion

        #region Submit Evaluation Tests

        [Test]
        public async Task SubmitEvaluation_ProposalDefense_ShouldSetMarksToZero()
        {
            // Arrange
            var project = TestDataFactory.CreateProject(1);
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.SubmitEvaluation(
                project.Id,
                "Proposal Defense",
                85,
                "Great presentation!"
            ) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Success"].ToString(), Is.EqualTo("Evaluation submitted successfully."));

            var evaluation = await _context.DefenseEvaluations.FirstOrDefaultAsync();
            Assert.That(evaluation, Is.Not.Null);
            Assert.That(evaluation.Marks, Is.EqualTo(0));
            Assert.That(evaluation.Feedback, Is.EqualTo("Great presentation!"));
        }

        [Test]
        public async Task SubmitEvaluation_InitialDefense_ShouldSaveMarks()
        {
            // Arrange
            var project = TestDataFactory.CreateProject(1);
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.SubmitEvaluation(
                project.Id,
                "Initial Defense",
                85,
                "Good work!"
            ) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);

            var evaluation = await _context.DefenseEvaluations.FirstOrDefaultAsync();
            Assert.That(evaluation, Is.Not.Null);
            Assert.That(evaluation.Marks, Is.EqualTo(85));
            Assert.That(evaluation.DefenseType, Is.EqualTo("Initial Defense"));
            Assert.That(evaluation.EvaluatorId, Is.EqualTo(_currentUserId));
        }

        [Test]
        public async Task SubmitEvaluation_ShouldUpdateAggregatedGrade()
        {
            // Arrange
            var project = TestDataFactory.CreateProject(1);
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Act - Submit first evaluation
            await _controller.SubmitEvaluation(project.Id, "Initial Defense", 85, "Good");

            // Submit second evaluation as another panel member
            var secondEvaluatorId = Guid.NewGuid().ToString();
            _userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(secondEvaluatorId);

            await _controller.SubmitEvaluation(project.Id, "Initial Defense", 75, "Needs improvement");

            // Assert
            var grade = await _context.ProjectGrades.FirstOrDefaultAsync(g => g.ProjectId == project.Id);
            Assert.That(grade, Is.Not.Null);
            Assert.That(grade.InitialDefenseMarks, Is.EqualTo(80));
        }

        #endregion
    }
}