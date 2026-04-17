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
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;

namespace FYP_App.Tests.Controllers
{
    [TestFixture]
    public class StudentControllerTests
    {
        private ApplicationDbContext _context;
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private Mock<IWebHostEnvironment> _webHostEnvironmentMock;
        private StudentController _controller;
        private string _currentUserId;

        [SetUp]
        public void Setup()
        {
            _currentUserId = Guid.NewGuid().ToString();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "StudentTestDb_" + Guid.NewGuid())
                .Options;
            _context = new ApplicationDbContext(options);

            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            _userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_currentUserId);

            _webHostEnvironmentMock = new Mock<IWebHostEnvironment>();
            _webHostEnvironmentMock.Setup(env => env.WebRootPath)
                .Returns(Path.GetTempPath());

            _controller = new StudentController(
                _userManagerMock.Object,
                _context,
                _webHostEnvironmentMock.Object);

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
        public async Task Index_WithProject_ShouldCalculateProgress()
        {
            // Arrange
            var settings = TestDataFactory.CreateActiveGlobalSettings();
            await _context.GlobalSettings.AddAsync(settings);

            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                StudentId = _currentUserId,
                Status = "Active"
            };
            await _context.Projects.AddAsync(project);

            var submission = new Submission
            {
                ProjectId = 1,
                SubmissionType = "Proposal",
                Status = "Approved",
                FilePath = "/test.pdf",
                SubmittedAt = DateTime.Now
            };
            await _context.Submissions.AddAsync(submission);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.ViewBag.Progress, Is.EqualTo(20));
            Assert.That(_controller.ViewBag.CurrentStage, Is.EqualTo("Proposal Approved"));
        }

        [Test]
        public async Task Index_NoProject_ShouldReturnViewWithNullProject()
        {
            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Model, Is.Null);
        }

        [Test]
        public async Task Index_WithAllDefensesComplete_ShouldShowCompleted()
        {
            // Arrange
            var project = new Project
            {
                Id = 1,
                Title = "Completed Project",
                StudentId = _currentUserId,
                Status = "Active"
            };
            await _context.Projects.AddAsync(project);

            var grade = new ProjectGrade
            {
                ProjectId = 1,
                InitialDefenseMarks = 85,
                MidtermDefenseMarks = 80,
                SupervisorMarks = 90,
                CoordinatorMarks = 88,
                FinalInternalMarks = 87,
                Grade = "A",
                TotalMarks = 86
            };
            await _context.ProjectGrades.AddAsync(grade);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.ViewBag.Progress, Is.EqualTo(100));
            Assert.That(_controller.ViewBag.CurrentStage, Is.EqualTo("Project Completed"));
            Assert.That(_controller.ViewBag.FinalGrade, Is.EqualTo("A"));
        }

        #endregion

        #region Upload Document Tests

        [Test]
        public async Task UploadDocument_ValidFile_ShouldSaveSubmission()
        {
            // Arrange
            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                StudentId = _currentUserId
            };
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            var file = TestDataFactory.CreateTestFormFile("test content", "proposal.pdf");

            // Act
            var result = await _controller.UploadDocument("Proposal", file) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ActionName, Is.EqualTo("SubmissionCenter"));
            Assert.That(_controller.TempData["Success"].ToString(), Does.Contain("uploaded successfully"));

            var submission = await _context.Submissions.FirstOrDefaultAsync();
            Assert.That(submission, Is.Not.Null);
            Assert.That(submission.SubmissionType, Is.EqualTo("Proposal"));
            Assert.That(submission.Status, Is.EqualTo("Pending Supervisor"));
            Assert.That(submission.ProjectId, Is.EqualTo(1));
        }

        [Test]
        public async Task UploadDocument_NoProject_ShouldRedirectToIndex()
        {
            // Arrange
            var file = TestDataFactory.CreateTestFormFile();

            // Act
            var result = await _controller.UploadDocument("Proposal", file) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ActionName, Is.EqualTo("Index"));
        }

        [Test]
        public async Task UploadDocument_NoFile_ShouldSetError()
        {
            // Arrange
            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                StudentId = _currentUserId
            };
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.UploadDocument("Proposal", null) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Error"].ToString(), Is.EqualTo("Please select a file."));
        }

        #endregion

        #region Delete Submission Tests

        [Test]
        public async Task DeleteSubmission_PendingSubmission_ShouldDelete()
        {
            // Arrange
            var submission = new Submission
            {
                Id = 1,
                ProjectId = 1,
                SubmissionType = "Proposal",
                Status = "Pending Supervisor",
                FilePath = "/uploads/submissions/test.pdf"
            };
            await _context.Submissions.AddAsync(submission);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteSubmission(1) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Success"].ToString(), Is.EqualTo("Submission deleted successfully."));

            var deleted = await _context.Submissions.FindAsync(1);
            Assert.That(deleted, Is.Null);
        }

        [Test]
        public async Task DeleteSubmission_ApprovedSubmission_ShouldNotDelete()
        {
            // Arrange
            var submission = new Submission
            {
                Id = 1,
                ProjectId = 1,
                SubmissionType = "Proposal",
                Status = "Approved",
                FilePath = "/uploads/submissions/test.pdf"
            };
            await _context.Submissions.AddAsync(submission);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteSubmission(1) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Error"].ToString(), Does.Contain("Cannot delete"));

            var stillExists = await _context.Submissions.FindAsync(1);
            Assert.That(stillExists, Is.Not.Null);
        }

        #endregion

        #region Meeting Logs Tests

        [Test]
        public async Task MeetingLogs_Get_ShouldReturnLogsAndPhaseInfo()
        {
            // Arrange
            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                StudentId = _currentUserId
            };
            await _context.Projects.AddAsync(project);

            var log1 = new MeetingLog
            {
                ProjectId = 1,
                MeetingNumber = 1,
                MeetingDate = DateTime.Now,
                StudentActivities = "Meeting 1",
                NextMeetingPlan = "Plan 1",
                Status = "Verified",
                Phase = "Initial"
            };
            await _context.MeetingLogs.AddAsync(log1);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.MeetingLogs() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var model = result.Model as List<MeetingLog>;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Count, Is.EqualTo(1));
            Assert.That(_controller.ViewBag.CurrentPhase, Is.EqualTo("Initial"));
            Assert.That(_controller.ViewBag.NextMeetingNo, Is.EqualTo(2));
            Assert.That(_controller.ViewBag.CanAdd, Is.True);
        }

        [Test]
        public async Task CreateLogEntry_ValidData_ShouldCreateLog()
        {
            // Arrange
            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                StudentId = _currentUserId
            };
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            var model = new MeetingLog
            {
                MeetingDate = DateTime.Now,
                StudentActivities = "Discussed requirements",
                NextMeetingPlan = "Complete literature review"
            };

            // Act
            var result = await _controller.CreateLogEntry(model) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Success"].ToString(), Does.Contain("saved successfully"));

            var log = await _context.MeetingLogs.FirstOrDefaultAsync();
            Assert.That(log, Is.Not.Null);
            Assert.That(log.MeetingNumber, Is.EqualTo(1));
            Assert.That(log.Status, Is.EqualTo("Pending"));
            Assert.That(log.Phase, Is.EqualTo("Initial"));
        }

        [Test]
        public async Task CreateLogEntry_MaxLogsReached_ShouldReturnError()
        {
            // Arrange
            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                StudentId = _currentUserId
            };
            await _context.Projects.AddAsync(project);

            // Add 24 logs (max)
            for (int i = 1; i <= 24; i++)
            {
                await _context.MeetingLogs.AddAsync(new MeetingLog
                {
                    ProjectId = 1,
                    MeetingNumber = i,
                    MeetingDate = DateTime.Now,
                    StudentActivities = $"Log {i}",
                    NextMeetingPlan = $"Plan {i}",
                    Status = "Verified"
                });
            }
            await _context.SaveChangesAsync();

            var model = new MeetingLog
            {
                MeetingDate = DateTime.Now,
                StudentActivities = "Extra log",
                NextMeetingPlan = "Extra plan"
            };

            // Act
            var result = await _controller.CreateLogEntry(model) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Error"].ToString(), Is.EqualTo("You have reached the maximum of 24 meeting logs."));
        }

        [Test]
        public async Task EditLogEntry_PendingLog_ShouldUpdate()
        {
            // Arrange
            var log = new MeetingLog
            {
                Id = 1,
                ProjectId = 1,
                MeetingNumber = 1,
                MeetingDate = DateTime.Now,
                StudentActivities = "Old activities",
                NextMeetingPlan = "Old plan",
                Status = "Pending"
            };
            await _context.MeetingLogs.AddAsync(log);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.EditLogEntry(1, "Updated activities", "Updated plan") as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Success"].ToString(), Is.EqualTo("Log entry updated successfully."));

            var updated = await _context.MeetingLogs.FindAsync(1);
            Assert.That(updated.StudentActivities, Is.EqualTo("Updated activities"));
            Assert.That(updated.NextMeetingPlan, Is.EqualTo("Updated plan"));
        }

        [Test]
        public async Task EditLogEntry_VerifiedLog_ShouldNotUpdate()
        {
            // Arrange
            var log = new MeetingLog
            {
                Id = 1,
                ProjectId = 1,
                MeetingNumber = 1,
                MeetingDate = DateTime.Now,
                StudentActivities = "Old activities",
                NextMeetingPlan = "Old plan",
                Status = "Verified"
            };
            await _context.MeetingLogs.AddAsync(log);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.EditLogEntry(1, "Updated activities", "Updated plan") as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Error"].ToString(), Does.Contain("Cannot edit"));

            var unchanged = await _context.MeetingLogs.FindAsync(1);
            Assert.That(unchanged.StudentActivities, Is.EqualTo("Old activities"));
        }

        [Test]
        public async Task DeleteLogEntry_PendingLog_ShouldDelete()
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
            var result = await _controller.DeleteLogEntry(1) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Success"].ToString(), Is.EqualTo("Log entry deleted."));

            var deleted = await _context.MeetingLogs.FindAsync(1);
            Assert.That(deleted, Is.Null);
        }

        [Test]
        public async Task DeleteLogEntry_VerifiedLog_ShouldNotDelete()
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
                Status = "Verified"
            };
            await _context.MeetingLogs.AddAsync(log);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteLogEntry(1) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.TempData["Error"].ToString(), Is.EqualTo("Cannot delete verified logs."));

            var stillExists = await _context.MeetingLogs.FindAsync(1);
            Assert.That(stillExists, Is.Not.Null);
        }

        #endregion

        #region Submission Center Tests

        [Test]
        public async Task SubmissionCenter_ShouldReturnProjectWithSubmissionsAndGrades()
        {
            // Arrange
            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                StudentId = _currentUserId
            };
            await _context.Projects.AddAsync(project);

            var submission = new Submission
            {
                ProjectId = 1,
                SubmissionType = "Proposal",
                Status = "Approved",
                FilePath = "/test.pdf"
            };
            await _context.Submissions.AddAsync(submission);

            var grade = new ProjectGrade
            {
                ProjectId = 1,
                InitialDefenseMarks = 85
            };
            await _context.ProjectGrades.AddAsync(grade);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.SubmissionCenter() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Model, Is.Not.Null);
            Assert.That(_controller.ViewBag.Grade, Is.Not.Null);
        }

        #endregion

        #region Download Templates Tests

        [Test]
        public async Task DownloadTemplates_ShouldReturnAllTemplates()
        {
            // Arrange
            var template1 = TestDataFactory.CreateDocumentTemplate("Template 1");
            var template2 = TestDataFactory.CreateDocumentTemplate("Template 2");
            await _context.DocumentTemplates.AddRangeAsync(template1, template2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DownloadTemplates() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var model = result.Model as List<DocumentTemplate>;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Count, Is.EqualTo(2));
        }

        #endregion
    }
}