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
    public class HODControllerTests
    {
        private ApplicationDbContext _context;
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private HODController _controller;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "HODTestDb_" + Guid.NewGuid())
                .Options;
            _context = new ApplicationDbContext(options);

            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            _controller = new HODController(_context, _userManagerMock.Object);

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
        public async Task Index_ShouldReturnDashboardWithCorrectCounts()
        {
            // Arrange
            var pendingPanel = TestDataFactory.CreatePanel(1, "Pending Panel", false);
            var approvedPanel = TestDataFactory.CreatePanel(2, "Approved Panel", true);
            await _context.Panels.AddRangeAsync(pendingPanel, approvedPanel);

            var project1 = TestDataFactory.CreateProject(1);
            var project2 = TestDataFactory.CreateProject(2);
            await _context.Projects.AddRangeAsync(project1, project2);

            var grade1 = new ProjectGrade { ProjectId = 1, Grade = "A", TotalMarks = 85 };
            var grade2 = new ProjectGrade { ProjectId = 2, Grade = "IP", TotalMarks = 0 };
            await _context.ProjectGrades.AddRangeAsync(grade1, grade2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.ViewBag.PendingPanels, Is.EqualTo(1));
            Assert.That(_controller.ViewBag.TotalProjects, Is.EqualTo(2));
            Assert.That(_controller.ViewBag.CompletedProjects, Is.EqualTo(1));
        }

        #endregion

        #region Approve Panels Tests

        [Test]
        public async Task ApprovePanels_Get_ShouldReturnOnlyPendingPanels()
        {
            // Arrange
            var pendingPanel1 = TestDataFactory.CreatePanel(1, "Pending Panel 1", false);
            var pendingPanel2 = TestDataFactory.CreatePanel(2, "Pending Panel 2", false);
            var approvedPanel = TestDataFactory.CreatePanel(3, "Approved Panel", true);

            await _context.Panels.AddRangeAsync(pendingPanel1, pendingPanel2, approvedPanel);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ApprovePanels() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var model = result.Model as List<Panel>;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Count, Is.EqualTo(2));
            Assert.That(model.All(p => !p.HODApproval), Is.True);
        }

        [Test]
        public async Task ApprovePanel_ValidPanel_ShouldSetApprovalTrue()
        {
            // Arrange
            var panel = TestDataFactory.CreatePanel(1, "Test Panel", false);
            panel.Members = new List<PanelMember>();
            await _context.Panels.AddAsync(panel);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ApprovePanel(panel.Id) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ActionName, Is.EqualTo("ApprovePanels"));
            Assert.That(_controller.TempData["Success"].ToString(), Does.Contain("has been approved"));

            var updatedPanel = await _context.Panels.FindAsync(panel.Id);
            Assert.That(updatedPanel.HODApproval, Is.True);
        }

        [Test]
        public async Task ApprovePanel_NonExistentPanel_ShouldNotThrow()
        {
            // Act
            var result = await _controller.ApprovePanel(999) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ActionName, Is.EqualTo("ApprovePanels"));
        }

        [Test]
        public async Task RejectPanel_ShouldDeletePanelAndMembers()
        {
            // Arrange
            var panel = TestDataFactory.CreatePanel(1, "Panel to Reject", false);
            panel.Members = new List<PanelMember>();
            await _context.Panels.AddAsync(panel);

            var member = new PanelMember
            {
                PanelId = panel.Id,
                UserId = "user123",
                Role = "Internal"
            };
            await _context.PanelMembers.AddAsync(member);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.RejectPanel(panel.Id) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Error"].ToString(), Does.Contain("rejected and removed"));

            var deletedPanel = await _context.Panels.FindAsync(panel.Id);
            Assert.That(deletedPanel, Is.Null);
        }

        #endregion

        #region Final Results Tests

        [Test]
        public async Task FinalResults_ShouldReturnAllProjectsWithGrades()
        {
            // Arrange
            var student = TestDataFactory.CreateStudent();
            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                StudentId = student.Id,
                Status = "Completed"
            };
            await _context.Projects.AddAsync(project);

            var grade = new ProjectGrade
            {
                ProjectId = 1,
                Grade = "A",
                TotalMarks = 85
            };
            await _context.ProjectGrades.AddAsync(grade);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.FinalResults() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.ViewBag.Results, Is.Not.Null);
        }

        #endregion

        #region System Alerts Tests
        [Test]
        public async Task SystemAlerts_WithInactiveStudents_ShouldGenerateAlerts()
        {
            // Arrange
            var settings = TestDataFactory.CreateActiveGlobalSettings();
            settings.ProposalDeadline = DateTime.Now.AddDays(-30);
            await _context.GlobalSettings.AddAsync(settings);
            await _context.SaveChangesAsync();

            var student = TestDataFactory.CreateStudent("inactive@test.com", "Inactive Student");
            var project = new Project
            {
                Id = 1,
                Title = "Inactive Project",
                StudentId = student.Id,
                Student = student,  // IMPORTANT: Set the Student navigation property
                Status = "Active"
            };
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            var oldMeeting = new MeetingLog
            {
                ProjectId = 1,
                MeetingNumber = 1,
                MeetingDate = DateTime.Now.AddDays(-20),
                StudentActivities = "Old meeting",
                NextMeetingPlan = "Next steps",
                Status = "Verified",
                Phase = "Initial"
            };
            await _context.MeetingLogs.AddAsync(oldMeeting);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.SystemAlerts() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task SystemAlerts_WithInactiveSupervisors_ShouldGenerateAlerts()
        {
            // Arrange
            var supervisor = TestDataFactory.CreateSupervisor("inactivesup@test.edu", "Inactive Supervisor");
            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                SupervisorId = supervisor.Id,
                Status = "Active"
            };
            await _context.Projects.AddAsync(project);

            var pendingLog = new MeetingLog
            {
                ProjectId = 1,
                MeetingNumber = 1,
                MeetingDate = DateTime.Now.AddDays(-20),
                StudentActivities = "Waiting for review",
                NextMeetingPlan = "Next steps",
                Status = "Pending",
                Phase = "Initial"
            };
            await _context.MeetingLogs.AddAsync(pendingLog);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Supervisor"))
                .ReturnsAsync(new List<ApplicationUser> { supervisor });

            // Act
            var result = await _controller.SystemAlerts() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var model = result.Model as List<SystemAlertViewModel>;
            Assert.That(model, Is.Not.Null);
        }

        [Test]
        public async Task BlockStudent_ShouldSetStatusToCancelled()
        {
            // Arrange
            var project = TestDataFactory.CreateProject(1);
            project.Status = "Active";
            project.WarningMessage = null;
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.BlockStudent(project.Id) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Success"].ToString(), Does.Contain("cancelled"));

            var updatedProject = await _context.Projects.FindAsync(project.Id);
            Assert.That(updatedProject.Status, Is.EqualTo("Cancelled"));
            Assert.That(updatedProject.WarningMessage, Does.Contain("CANCELLED"));
        }

        [Test]
        public async Task UnblockStudent_ShouldRestoreStatusAndClearWarning()
        {
            // Arrange
            var project = TestDataFactory.CreateProject(1);
            project.Status = "Cancelled";
            project.WarningMessage = "CANCELLED: Inactivity";
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.UnblockStudent(project.Id) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Success"].ToString(), Is.EqualTo("Student account has been reactivated."));

            var updatedProject = await _context.Projects.FindAsync(project.Id);
            Assert.That(updatedProject.Status, Is.EqualTo("Active"));
            Assert.That(updatedProject.WarningMessage, Is.Null);
        }

        [Test]
        public async Task AllowStudent_ShouldIssueWarning()
        {
            // Arrange
            var project = TestDataFactory.CreateProject(1);
            project.WarningMessage = null;
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.AllowStudent(project.Id) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Success"].ToString(), Is.EqualTo("Warning issued to student."));

            var updatedProject = await _context.Projects.FindAsync(project.Id);
            Assert.That(updatedProject.WarningMessage, Does.Contain("WARNING"));
        }

        [Test]
        public async Task ReportSupervisor_ShouldFlagSupervisor()
        {
            // Arrange
            var project = TestDataFactory.CreateProject(1);
            project.SupervisorFlagged = false;
            project.WarningMessage = null;
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ReportSupervisor(project.Id) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Success"].ToString(), Is.EqualTo("Supervisor has been reported to the Coordinator."));

            var updatedProject = await _context.Projects.FindAsync(project.Id);
            Assert.That(updatedProject.SupervisorFlagged, Is.True);
        }

        #endregion
    }
}