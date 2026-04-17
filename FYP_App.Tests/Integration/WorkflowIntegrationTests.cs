using FluentAssertions;
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
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FYP_App.Tests.Integration
{
    [TestFixture]
    [Ignore("Integration tests require additional setup")]

    public class WorkflowIntegrationTests
    {
        private ApplicationDbContext _context;
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private Mock<IWebHostEnvironment> _webHostEnvironmentMock;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"IntegrationTestDb_{Guid.NewGuid()}")
                .Options;
            _context = new ApplicationDbContext(options);

            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
                _userManagerMock.Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
                null, null, null, null);

            _webHostEnvironmentMock = new Mock<IWebHostEnvironment>();
            _webHostEnvironmentMock.Setup(env => env.WebRootPath)
                .Returns(Path.GetTempPath());
        }
            private void SetupControllerTempData(Controller controller)
            {
                controller.TempData = new TempDataDictionary(
                    new DefaultHttpContext(),
                    Mock.Of<ITempDataProvider>());
            }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Complete Registration to Project Creation Flow

        [Test]
        public async Task CompleteRegistrationToProjectCreationFlow_ShouldSucceed()
        {
            // Step 1: Setup active registration
            var settings = TestDataFactory.CreateActiveGlobalSettings();
            await _context.GlobalSettings.AddAsync(settings);
            await _context.SaveChangesAsync();

            // Step 2: Student submits registration
            var accountController = new AccountController(
                _signInManagerMock.Object,
                _userManagerMock.Object,
                _context);

            var registration = TestDataFactory.CreateValidFypRegistration();
            var registerResult = await accountController.RegisterGroup(registration);
            registerResult.Should().BeOfType<ViewResult>();

            // Step 3: Coordinator approves registration
            var coordinatorController = new CoordinatorController(
                _userManagerMock.Object,
                _context,
                _webHostEnvironmentMock.Object,
                _webHostEnvironmentMock.Object)
            {
                TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                    new DefaultHttpContext(),
                    Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>())
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(registration.Student1Email))
                .ReturnsAsync((ApplicationUser)null);
            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Student"))
                .ReturnsAsync(IdentityResult.Success);

            var savedRegistration = await _context.FypRegistrations.FirstOrDefaultAsync();
            var approveResult = await coordinatorController.ApproveRegistration(savedRegistration.Id);
            approveResult.Should().BeOfType<RedirectToActionResult>();

            // Step 4: Verify project was created
            var project = await _context.Projects.FirstOrDefaultAsync();
            project.Should().NotBeNull();
            project.Title.Should().Be(registration.ProposedTitle);
            project.Status.Should().Be("Approved");

            // Step 5: Verify registration marked as processed
            var updatedRegistration = await _context.FypRegistrations.FindAsync(savedRegistration.Id);
            updatedRegistration.IsProcessed.Should().BeTrue();
        }

        #endregion

        #region Document Submission to Approval Flow

        [Test]
        public async Task DocumentSubmissionToApprovalFlow_ShouldWorkCorrectly()
        {
            // Setup: Create approved project with student and supervisor
            var studentId = Guid.NewGuid().ToString();
            var supervisorId = Guid.NewGuid().ToString();

            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                StudentId = studentId,
                SupervisorId = supervisorId,
                Status = "Approved"
            };
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Setup Student Controller
            _userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(studentId);

            var studentController = new StudentController(
                _userManagerMock.Object,
                _context,
                _webHostEnvironmentMock.Object);

            SetupControllerContext(studentController, studentId);

            // Step 1: Student uploads document
            var file = TestDataFactory.CreateTestFormFile("Proposal content", "proposal.pdf");
            var uploadResult = await studentController.UploadDocument("Proposal", file);
            uploadResult.Should().BeOfType<RedirectToActionResult>();

            var submission = await _context.Submissions.FirstOrDefaultAsync();
            submission.Should().NotBeNull();
            submission.Status.Should().Be("Pending Supervisor");

            // Step 2: Supervisor reviews and approves
            _userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(supervisorId);

            var supervisorController = new SupervisorController(_userManagerMock.Object, _context);
            SetupControllerContext(supervisorController, supervisorId);

            var reviewResult = await supervisorController.SubmitReview(submission.Id, "Approve", "Good work!");
            reviewResult.Should().BeOfType<RedirectToActionResult>();

            var updatedSubmission = await _context.Submissions.FindAsync(submission.Id);
            updatedSubmission.Status.Should().Be("Pending Coordinator");

            // Step 3: Coordinator reviews and finalizes
            var coordinatorController = new CoordinatorController(
                _userManagerMock.Object,
                _context,
                _webHostEnvironmentMock.Object,
                _webHostEnvironmentMock.Object)
            {
                TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                    new DefaultHttpContext(),
                    Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>())
            };

            var finalizeResult = await coordinatorController.ReviewSubmission(
                updatedSubmission.Id,
                "Finalize",
                "Approved by Coordinator");

            finalizeResult.Should().BeOfType<RedirectToActionResult>();

            var finalizedSubmission = await _context.Submissions.FindAsync(submission.Id);
            finalizedSubmission.Status.Should().Be("Approved");
        }

        #endregion

        #region Meeting Log Creation to Verification Flow

        [Test]
        public async Task MeetingLogCreationToVerificationFlow_ShouldWorkCorrectly()
        {
            // Setup
            var studentId = Guid.NewGuid().ToString();
            var supervisorId = Guid.NewGuid().ToString();

            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                StudentId = studentId,
                SupervisorId = supervisorId,
                Status = "Active"
            };
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Step 1: Student creates meeting log
            _userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(studentId);

            var studentController = new StudentController(
                _userManagerMock.Object,
                _context,
                _webHostEnvironmentMock.Object);
            SetupControllerContext(studentController, studentId);

            var logEntry = new MeetingLog
            {
                MeetingDate = DateTime.Now,
                StudentActivities = "Discussed project progress",
                NextMeetingPlan = "Complete module 1 by next week"
            };

            var createResult = await studentController.CreateLogEntry(logEntry);
            createResult.Should().BeOfType<RedirectToActionResult>();

            var log = await _context.MeetingLogs.FirstOrDefaultAsync();
            log.Should().NotBeNull();
            log.Status.Should().Be("Pending");
            log.MeetingNumber.Should().Be(1);

            // Step 2: Supervisor verifies log
            _userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(supervisorId);

            var supervisorController = new SupervisorController(_userManagerMock.Object, _context);
            SetupControllerContext(supervisorController, supervisorId);

            var verifyResult = await supervisorController.VerifyLog(log.Id, "Approve", "Good progress!");
            verifyResult.Should().BeOfType<RedirectToActionResult>();

            var verifiedLog = await _context.MeetingLogs.FindAsync(log.Id);
            verifiedLog.Status.Should().Be("Verified");
            verifiedLog.SupervisorComments.Should().Be("Good progress!");
        }

        #endregion

        #region Panel Creation to Defense Scheduling Flow

        [Test]
        public async Task PanelCreationToDefenseSchedulingFlow_ShouldWorkCorrectly()
        {
            // Setup
            var coordinatorId = Guid.NewGuid().ToString();
            var hodId = Guid.NewGuid().ToString();

            var coordinatorController = new CoordinatorController(
                _userManagerMock.Object,
                _context,
                _webHostEnvironmentMock.Object,
                _webHostEnvironmentMock.Object)
            {
                TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                    new DefaultHttpContext(),
                    Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>())
            };

            var hodController = new HODController(_context, _userManagerMock.Object)
            {
                TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                    new DefaultHttpContext(),
                    Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>())
            };

            // Step 1: Coordinator creates panel
            var memberIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };

            _userManagerMock.Setup(um => um.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new ApplicationUser { Id = memberIds[0], FullName = "Panel Member" });
            _userManagerMock.Setup(um => um.GetClaimsAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<Claim> { new Claim("PanelistType", "Internal") });

            var createPanelResult = await coordinatorController.CreatePanel("Defense Panel", memberIds);
            createPanelResult.Should().BeOfType<RedirectToActionResult>();

            var panel = await _context.Panels.FirstOrDefaultAsync();
            panel.Should().NotBeNull();
            panel.HODApproval.Should().BeFalse();

            // Step 2: HOD approves panel
            var approvePanelResult = await hodController.ApprovePanel(panel.Id);
            approvePanelResult.Should().BeOfType<RedirectToActionResult>();

            var approvedPanel = await _context.Panels.FindAsync(panel.Id);
            approvedPanel.HODApproval.Should().BeTrue();

            // Step 3: Create project for defense
            var project = new Project
            {
                Id = 1,
                Title = "Defense Project",
                StudentId = Guid.NewGuid().ToString(),
                Status = "Active"
            };
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Step 4: Coordinator schedules defense
            var scheduleResult = await coordinatorController.CreateSchedule(
                project.Id,
                panel.Id,
                "Final Defense",
                DateTime.Now.AddDays(14),
                "Room 301");

            scheduleResult.Should().BeOfType<RedirectToActionResult>();

            var schedule = await _context.DefenseSchedules.FirstOrDefaultAsync();
            schedule.Should().NotBeNull();
            schedule.ProjectId.Should().Be(project.Id);
            schedule.PanelId.Should().Be(panel.Id);
            schedule.DefenseType.Should().Be("Final Defense");
        }

        #endregion

        #region Defense Evaluation to Grade Calculation Flow

        [Test]
        public async Task DefenseEvaluationToGradeCalculationFlow_ShouldWorkCorrectly()
        {
            // Setup project, panel, and defense schedule
            var project = new Project
            {
                Id = 1,
                Title = "Evaluation Project",
                StudentId = Guid.NewGuid().ToString(),
                Status = "Active"
            };
            await _context.Projects.AddAsync(project);

            var panel = TestDataFactory.CreatePanel(1, "Evaluation Panel", true);
            await _context.Panels.AddAsync(panel);

            var defense = new DefenseSchedule
            {
                Id = 1,
                ProjectId = 1,
                PanelId = 1,
                DefenseType = "Final Defense",
                Date = DateTime.Now,
                Room = "Room 101"
            };
            await _context.DefenseSchedules.AddAsync(defense);
            await _context.SaveChangesAsync();

            // Step 1: Internal panel member submits evaluation
            var internalMemberId = Guid.NewGuid().ToString();
            _userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(internalMemberId);

            var internalMember = new PanelMember
            {
                PanelId = 1,
                UserId = internalMemberId,
                Role = "Internal"
            };
            await _context.PanelMembers.AddAsync(internalMember);
            await _context.SaveChangesAsync();

            var panelController = new PanelController(_userManagerMock.Object, _context);
            SetupControllerContext(panelController, internalMemberId);

            var internalEvalResult = await panelController.SubmitEvaluation(
                1, "Final Defense", 88, "Excellent technical work");
            internalEvalResult.Should().BeOfType<RedirectToActionResult>();

            // Step 2: External panel member submits evaluation
            var externalMemberId = Guid.NewGuid().ToString();
            _userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(externalMemberId);

            var externalMember = new PanelMember
            {
                PanelId = 1,
                UserId = externalMemberId,
                Role = "External"
            };
            await _context.PanelMembers.AddAsync(externalMember);
            await _context.SaveChangesAsync();

            SetupControllerContext(panelController, externalMemberId);

            var externalEvalResult = await panelController.SubmitEvaluation(
                1, "Final Defense", 92, "Outstanding presentation");
            externalEvalResult.Should().BeOfType<RedirectToActionResult>();

            // Step 3: Coordinator updates final grades
            var coordinatorController = new CoordinatorController(
                _userManagerMock.Object,
                _context,
                _webHostEnvironmentMock.Object,
                _webHostEnvironmentMock.Object)
            {
                TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                    new DefaultHttpContext(),
                    Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>())
            };

            var gradeResult = await coordinatorController.UpdateGrades(1, 90, "Final");
            gradeResult.Should().BeOfType<RedirectToActionResult>();

            // Step 4: Verify final grade calculation
            var grade = await _context.ProjectGrades.FirstOrDefaultAsync(g => g.ProjectId == 1);
            grade.Should().NotBeNull();
            grade.FinalInternalMarks.Should().Be(88);
            grade.FinalExternalMarks.Should().Be(92);
            grade.CoordinatorMarks.Should().Be(90);
        }

        #endregion

        #region Inactivity Detection to Student Blocking Flow

        [Test]
        public async Task InactivityDetectionToStudentBlockingFlow_ShouldWorkCorrectly()
        {
            // Setup: Create project with inactivity
            var studentId = Guid.NewGuid().ToString();
            var project = new Project
            {
                Id = 1,
                Title = "Inactive Project",
                StudentId = studentId,
                Status = "Active",
                SupervisorFlagged = false
            };
            await _context.Projects.AddAsync(project);

            var settings = TestDataFactory.CreateActiveGlobalSettings();
            settings.ProposalDeadline = DateTime.Now.AddDays(-30);
            await _context.GlobalSettings.AddAsync(settings);

            var oldMeeting = new MeetingLog
            {
                ProjectId = 1,
                MeetingNumber = 1,
                MeetingDate = DateTime.Now.AddDays(-20),
                StudentActivities = "Old meeting",
                NextMeetingPlan = "Plan",
                Status = "Verified"
            };
            await _context.MeetingLogs.AddAsync(oldMeeting);
            await _context.SaveChangesAsync();

            // Step 1: HOD views system alerts
            var hodController = new HODController(_context, _userManagerMock.Object)
            {
                TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                    new DefaultHttpContext(),
                    Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>())
            };

            var alertsResult = await hodController.SystemAlerts();
            alertsResult.Should().BeOfType<ViewResult>();

            // Step 2: HOD blocks inactive student
            var blockResult = await hodController.BlockStudent(1);
            blockResult.Should().BeOfType<RedirectToActionResult>();

            var blockedProject = await _context.Projects.FindAsync(1);
            blockedProject.Status.Should().Be("Cancelled");
            blockedProject.WarningMessage.Should().Contain("CANCELLED");

            // Step 3: HOD can unblock student
            var unblockResult = await hodController.UnblockStudent(1);
            unblockResult.Should().BeOfType<RedirectToActionResult>();

            var unblockedProject = await _context.Projects.FindAsync(1);
            unblockedProject.Status.Should().Be("Active");
            unblockedProject.WarningMessage.Should().BeNull();
        }

        #endregion

        #region Helper Methods

        private void SetupControllerContext(Controller controller, string userId)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        #endregion
    }
}