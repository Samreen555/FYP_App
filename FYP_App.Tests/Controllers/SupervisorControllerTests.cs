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
    public class SupervisorControllerTests
    {
        private ApplicationDbContext _context;
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private SupervisorController _controller;
        private string _currentUserId;

        [SetUp]
        public void Setup()
        {
            _currentUserId = Guid.NewGuid().ToString();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "SupervisorTestDb_" + Guid.NewGuid())
                .Options;
            _context = new ApplicationDbContext(options);

            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            _userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_currentUserId);

            _controller = new SupervisorController(_userManagerMock.Object, _context);

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
        public async Task Index_ShouldReturnMyProjectsWithCounts()
        {
            // Arrange
            var student = TestDataFactory.CreateStudent();
            var project1 = new Project
            {
                Id = 1,
                Title = "Project 1",
                SupervisorId = _currentUserId,
                StudentId = student.Id
            };
            var project2 = new Project
            {
                Id = 2,
                Title = "Project 2",
                SupervisorId = _currentUserId,
                StudentId = student.Id
            };
            await _context.Projects.AddRangeAsync(project1, project2);

            var settings = TestDataFactory.CreateActiveGlobalSettings();
            await _context.GlobalSettings.AddAsync(settings);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var model = result.Model as List<Project>;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Count, Is.EqualTo(2));
            Assert.That(_controller.ViewBag.MyProjectsCount, Is.EqualTo(2));
        }

        #endregion

        #region My Projects Tests

        [Test]
        public async Task MyProjects_ShouldReturnOnlyAssignedProjects()
        {
            // Arrange
            var project1 = new Project
            {
                Id = 1,
                Title = "My Project",
                SupervisorId = _currentUserId
            };
            var project2 = new Project
            {
                Id = 2,
                Title = "Other Project",
                SupervisorId = "other-supervisor-id"
            };
            await _context.Projects.AddRangeAsync(project1, project2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.MyProjects() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var model = result.Model as List<Project>;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Count, Is.EqualTo(1));
            Assert.That(model[0].Title, Is.EqualTo("My Project"));
        }

        #endregion

        #region Review Submissions Tests

        [Test]
        public async Task ReviewSubmissions_Get_ShouldReturnProjectsWithSubmissions()
        {
            // Arrange
            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                SupervisorId = _currentUserId
            };
            await _context.Projects.AddAsync(project);

            var submission = new Submission
            {
                Id = 1,
                ProjectId = 1,
                SubmissionType = "Proposal",
                Status = "Pending Supervisor",
                FilePath = "/test.pdf"
            };
            await _context.Submissions.AddAsync(submission);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ReviewSubmissions() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var model = result.Model as List<Project>;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task SubmitReview_Approve_ShouldSetStatusToPendingCoordinator()
        {
            // Arrange
            var submission = new Submission
            {
                Id = 1,
                ProjectId = 1,
                SubmissionType = "Proposal",
                Status = "Pending Supervisor",
                FilePath = "/test.pdf"
            };
            await _context.Submissions.AddAsync(submission);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.SubmitReview(1, "Approve", "Good work!") as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Success"].ToString(), Does.Contain("forwarded to Coordinator"));

            var updated = await _context.Submissions.FindAsync(1);
            Assert.That(updated.Status, Is.EqualTo("Pending Coordinator"));
            Assert.That(updated.Remarks, Is.EqualTo("Good work!"));
        }

        [Test]
        public async Task SubmitReview_Reject_ShouldSetStatusToRejected()
        {
            // Arrange
            var submission = new Submission
            {
                Id = 1,
                ProjectId = 1,
                SubmissionType = "Proposal",
                Status = "Pending Supervisor",
                FilePath = "/test.pdf"
            };
            await _context.Submissions.AddAsync(submission);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.SubmitReview(1, "Reject", "Needs improvement") as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Success"].ToString(), Does.Contain("forwarded to Coordinator"));

            var updated = await _context.Submissions.FindAsync(1);
            Assert.That(updated.Status, Is.EqualTo("Rejected"));
            Assert.That(updated.Remarks, Is.EqualTo("Needs improvement"));
        }

        [Test]
        public async Task SubmitReview_FeedbackOnly_ShouldKeepStatusAndUpdateRemarks()
        {
            // Arrange
            var submission = new Submission
            {
                Id = 1,
                ProjectId = 1,
                SubmissionType = "Proposal",
                Status = "Pending Supervisor",
                FilePath = "/test.pdf"
            };
            await _context.Submissions.AddAsync(submission);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.SubmitReview(1, "FeedbackOnly", "Please add more details") as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Success"].ToString(), Does.Contain("Feedback updated"));

            var updated = await _context.Submissions.FindAsync(1);
            Assert.That(updated.Status, Is.EqualTo("Pending Supervisor"));
            Assert.That(updated.Remarks, Is.EqualTo("Please add more details"));
        }

        #endregion

        #region Meeting Logs Tests

        [Test]
        public async Task MeetingLogs_Get_ShouldReturnProjectsWithLogs()
        {
            // Arrange
            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                SupervisorId = _currentUserId
            };
            await _context.Projects.AddAsync(project);

            var log = new MeetingLog
            {
                Id = 1,
                ProjectId = 1,
                MeetingNumber = 1,
                MeetingDate = DateTime.Now,
                StudentActivities = "Activities",
                NextMeetingPlan = "Plan",
                Status = "Pending"
            };
            await _context.MeetingLogs.AddAsync(log);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.MeetingLogs() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var model = result.Model as List<Project>;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task VerifyLog_Approve_ShouldSetStatusToVerified()
        {
            // Arrange
            var log = new MeetingLog
            {
                Id = 1,
                ProjectId = 1,
                MeetingNumber = 1,
                MeetingDate = DateTime.Now,
                StudentActivities = "Activities",
                NextMeetingPlan = "Plan",
                Status = "Pending"
            };
            await _context.MeetingLogs.AddAsync(log);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.VerifyLog(1, "Approve", "Well done!") as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Success"].ToString(), Does.Contain("Log #1 updated"));

            var updated = await _context.MeetingLogs.FindAsync(1);
            Assert.That(updated.Status, Is.EqualTo("Verified"));
            Assert.That(updated.SupervisorComments, Is.EqualTo("Well done!"));
        }

        [Test]
        public async Task VerifyLog_Reject_ShouldSetStatusToRejected()
        {
            // Arrange
            var log = new MeetingLog
            {
                Id = 1,
                ProjectId = 1,
                MeetingNumber = 1,
                MeetingDate = DateTime.Now,
                StudentActivities = "Activities",
                NextMeetingPlan = "Plan",
                Status = "Pending"
            };
            await _context.MeetingLogs.AddAsync(log);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.VerifyLog(1, "Reject", "Insufficient detail") as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Success"].ToString(), Does.Contain("Log #1 updated"));

            var updated = await _context.MeetingLogs.FindAsync(1);
            Assert.That(updated.Status, Is.EqualTo("Rejected"));
            Assert.That(updated.SupervisorComments, Is.EqualTo("Insufficient detail"));
        }

        #endregion

        #region Final Grading Tests

        [Test]
        public async Task FinalGrading_Get_ShouldReturnProjectsWithGrades()
        {
            // Arrange
            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                SupervisorId = _currentUserId
            };
            await _context.Projects.AddAsync(project);

            var defense = new DefenseSchedule
            {
                Id = 1,
                ProjectId = 1,
                PanelId = 1,
                DefenseType = "Final Defense",
                Date = DateTime.Today,
                Room = "Room 101"
            };
            await _context.DefenseSchedules.AddAsync(defense);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.FinalGrading() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task SaveFinalGrade_OnDefenseDay_ShouldSaveMarks()
        {
            // Arrange
            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                SupervisorId = _currentUserId
            };
            await _context.Projects.AddAsync(project);

            var defense = new DefenseSchedule
            {
                Id = 1,
                ProjectId = 1,
                PanelId = 1,
                DefenseType = "Final Defense",
                Date = DateTime.Today,
                Room = "Room 101"
            };
            await _context.DefenseSchedules.AddAsync(defense);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.SaveFinalGrade(1, 85) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Success"].ToString(), Is.EqualTo("Marks saved."));

            var grade = await _context.ProjectGrades.FirstOrDefaultAsync(g => g.ProjectId == 1);
            Assert.That(grade, Is.Not.Null);
            Assert.That(grade.SupervisorMarks, Is.EqualTo(85));
        }

        [Test]
        public async Task SaveFinalGrade_NotOnDefenseDay_ShouldReturnError()
        {
            // Arrange
            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                SupervisorId = _currentUserId
            };
            await _context.Projects.AddAsync(project);

            var defense = new DefenseSchedule
            {
                Id = 1,
                ProjectId = 1,
                PanelId = 1,
                DefenseType = "Final Defense",
                Date = DateTime.Today.AddDays(1),
                Room = "Room 101"
            };
            await _context.DefenseSchedules.AddAsync(defense);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.SaveFinalGrade(1, 85) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Error"].ToString(), Is.EqualTo("Grading is only allowed on the day of the Final Defense."));
        }

        [Test]
        public async Task SaveFinalGrade_NoDefenseScheduled_ShouldReturnError()
        {
            // Arrange
            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                SupervisorId = _currentUserId
            };
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.SaveFinalGrade(1, 85) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Error"].ToString(), Is.EqualTo("Grading is only allowed on the day of the Final Defense."));
        }

        #endregion
    }
}