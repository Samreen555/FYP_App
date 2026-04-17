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
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace FYP_App.Tests.Controllers
{
    [TestFixture]
    public class CoordinatorControllerTests
    {
        private ApplicationDbContext _context;
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private Mock<IWebHostEnvironment> _webHostEnvironmentMock;
        private CoordinatorController _controller;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "CoordinatorTestDb_" + Guid.NewGuid())
                .Options;
            _context = new ApplicationDbContext(options);

            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            _webHostEnvironmentMock = new Mock<IWebHostEnvironment>();
            _webHostEnvironmentMock.Setup(env => env.WebRootPath)
                .Returns(Path.GetTempPath());

            // Setup TempData
            var tempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());

            _controller = new CoordinatorController(
                _userManagerMock.Object,
                _context,
                _webHostEnvironmentMock.Object,
                _webHostEnvironmentMock.Object)
            {
                TempData = tempData
            };

            // Setup ControllerContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }
        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Dashboard Tests

        [Test]
        public async Task Index_ShouldReturnDashboardWithCorrectCounts()
        {
            // Arrange
            var students = new List<ApplicationUser>
            {
                TestDataFactory.CreateStudent("s1@test.com", "Student 1"),
                TestDataFactory.CreateStudent("s2@test.com", "Student 2")
            };
            var supervisors = new List<ApplicationUser>
            {
                TestDataFactory.CreateSupervisor("sup1@test.com", "Supervisor 1")
            };

            _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Student"))
                .ReturnsAsync(students);
            _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Supervisor"))
                .ReturnsAsync(supervisors);

            var pendingReg = TestDataFactory.CreateValidFypRegistration();
            await _context.FypRegistrations.AddAsync(pendingReg);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            result.Should().NotBeNull();
            _controller.ViewBag.StudentCount.Should().Be(2);
            _controller.ViewBag.SupervisorCount.Should().Be(1);
            _controller.ViewBag.PendingRegistrations.Should().Be(1);
        }

        #endregion

        #region Registration Approval Tests

        [Test]
        public async Task PendingRegistrations_ShouldReturnUnprocessedOnly()
        {
            // Arrange
            var processed = TestDataFactory.CreateValidFypRegistration(1);
            processed.IsProcessed = true;
            var pending = TestDataFactory.CreateValidFypRegistration(2);
            pending.Student1Email = "pending@test.com";

            await _context.FypRegistrations.AddRangeAsync(processed, pending);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.PendingRegistrations() as ViewResult;

            // Assert
            result.Should().NotBeNull();
            var model = result.Model as List<FypRegistration>;
            model.Should().HaveCount(1);
            model[0].IsProcessed.Should().BeFalse();
        }

        [Test]
        public async Task ApproveRegistration_ValidRegistration_ShouldCreateUserAndProject()
        {
            // Arrange
            var registration = TestDataFactory.CreateValidFypRegistration(1);
            await _context.FypRegistrations.AddAsync(registration);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(um => um.FindByEmailAsync(registration.Student1Email))
                .ReturnsAsync((ApplicationUser)null);
            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Student"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.ApproveRegistration(registration.Id) as RedirectToActionResult;

            // Assert
            result.Should().NotBeNull();
            result.ActionName.Should().Be("ShowCredentials");

            var updatedReg = await _context.FypRegistrations.FindAsync(registration.Id);
            updatedReg.IsProcessed.Should().BeTrue();

            var project = await _context.Projects.FirstOrDefaultAsync();
            project.Should().NotBeNull();
            project.Title.Should().Be(registration.ProposedTitle);
            project.Status.Should().Be("Approved");
        }

        [Test]
        public async Task ApproveRegistration_UserAlreadyExists_ShouldResetPassword()
        {
            // Arrange
            var registration = TestDataFactory.CreateValidFypRegistration(1);
            await _context.FypRegistrations.AddAsync(registration);
            await _context.SaveChangesAsync();

            var existingUser = TestDataFactory.CreateStudent(registration.Student1Email, registration.Student1Name);

            _userManagerMock.Setup(um => um.FindByEmailAsync(registration.Student1Email))
                .ReturnsAsync(existingUser);
            _userManagerMock.Setup(um => um.GeneratePasswordResetTokenAsync(existingUser))
                .ReturnsAsync("reset-token");
            _userManagerMock.Setup(um => um.ResetPasswordAsync(existingUser, "reset-token", It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(um => um.IsInRoleAsync(existingUser, "Student"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ApproveRegistration(registration.Id) as RedirectToActionResult;

            // Assert
            result.Should().NotBeNull();
            _userManagerMock.Verify(um => um.ResetPasswordAsync(existingUser, "reset-token", It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void ShowCredentials_WithTempData_ShouldReturnView()
        {
            // Arrange
            _controller.TempData["NewEmail"] = "test@student.edu";
            _controller.TempData["NewPassword"] = "Test123!";
            _controller.TempData["NewName"] = "Test Student";

            // Act
            var result = _controller.ShowCredentials() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ShowCredentials_WithoutTempData_ShouldRedirectToIndex()
        {
            // Act
            var result = _controller.ShowCredentials() as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ActionName, Is.EqualTo("Index"));
        }


        #endregion

        #region Manage Projects & Assign Supervisor Tests

        [Test]
        public async Task ManageProjects_ShouldReturnAllProjects()
        {
            // Arrange
            var project1 = TestDataFactory.CreateProject(1);
            var project2 = TestDataFactory.CreateProject(2);
            await _context.Projects.AddRangeAsync(project1, project2);
            await _context.SaveChangesAsync();

            var supervisors = new List<ApplicationUser> { TestDataFactory.CreateSupervisor() };
            _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Supervisor"))
                .ReturnsAsync(supervisors);

            // Act
            var result = await _controller.ManageProjects() as ViewResult;

            // Assert
            result.Should().NotBeNull();
            var model = result.Model as List<Project>;
            model.Should().HaveCount(2);
        }

        [Test]
        public async Task AssignSupervisor_ValidData_ShouldUpdateProject()
        {
            // Arrange
            var project = TestDataFactory.CreateProject(1);
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            var supervisor = TestDataFactory.CreateSupervisor("sup@test.edu", "Dr. Smith");
            _userManagerMock.Setup(um => um.FindByIdAsync(supervisor.Id))
                .ReturnsAsync(supervisor);

            // Act
            var result = await _controller.AssignSupervisor(project.Id, supervisor.Id) as RedirectToActionResult;

            // Assert
            result.Should().NotBeNull();
            _controller.TempData["Success"].Should().NotBeNull();

            var updatedProject = await _context.Projects.FindAsync(project.Id);
            updatedProject.SupervisorId.Should().Be(supervisor.Id);
            updatedProject.Status.Should().Be("Assigned");
        }

        [Test]
        public async Task AssignSupervisor_InvalidSupervisor_ShouldReturnError()
        {
            // Arrange
            var project = TestDataFactory.CreateProject(1);
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(um => um.FindByIdAsync("invalid-id"))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.AssignSupervisor(project.Id, "invalid-id") as RedirectToActionResult;

            // Assert
            result.Should().NotBeNull();
            _controller.TempData["Error"].Should().Be("Supervisor not found.");
        }

        #endregion

        #region Manage Panels Tests

        [Test]
        public async Task ManagePanels_ShouldReturnAllPanelsWithFacultyList()
        {
            // Arrange
            var panel = TestDataFactory.CreatePanel(1, "Panel A");
            await _context.Panels.AddAsync(panel);
            await _context.SaveChangesAsync();

            // Create a list instead of IQueryable for async operations
            var panelUsers = new List<ApplicationUser> { TestDataFactory.CreatePanelMember() };

            // Use the list directly since Users property might not support async
            _userManagerMock.Setup(um => um.Users).Returns(panelUsers.AsQueryable());
            _userManagerMock.Setup(um => um.IsInRoleAsync(It.IsAny<ApplicationUser>(), "Panel"))
                .ReturnsAsync(true);

            // Mock GetClaimsAsync
            _userManagerMock.Setup(um => um.GetClaimsAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<System.Security.Claims.Claim>());

            // Act
            var result = await _controller.ManagePanels() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var model = result.Model as List<Panel>;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Count, Is.EqualTo(1));
        }
        [Test]
        public async Task CreatePanel_ValidData_ShouldCreatePanel()
        {
            // Arrange
            var memberIds = new List<string> { Guid.NewGuid().ToString() };
            var user = TestDataFactory.CreatePanelMember();

            _userManagerMock.Setup(um => um.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.GetClaimsAsync(user))
                .ReturnsAsync(new List<Claim> { new Claim("PanelistType", "Internal") });

            // Act
            var result = await _controller.CreatePanel("New Panel", memberIds) as RedirectToActionResult;

            // Assert
            result.Should().NotBeNull();
            result.ActionName.Should().Be("ManagePanels");

            var panel = await _context.Panels.FirstOrDefaultAsync();
            panel.Should().NotBeNull();
            panel.Name.Should().Be("New Panel");
            panel.HODApproval.Should().BeFalse();

            var members = await _context.PanelMembers.CountAsync();
            members.Should().Be(1);
        }

        [Test]
        public async Task CreatePanel_EmptyName_ShouldReturnError()
        {
            // Arrange
            var memberIds = new List<string> { "user1" };

            // Act
            var result = await _controller.CreatePanel("", memberIds) as RedirectToActionResult;

            // Assert
            result.Should().NotBeNull();
            _controller.TempData["Error"].Should().Be("Panel Name and at least one member are required.");
        }

        [Test]
        public async Task DeletePanel_NotScheduled_ShouldDeletePanel()
        {
            // Arrange
            var panel = TestDataFactory.CreatePanel(1, "Panel to Delete");
            await _context.Panels.AddAsync(panel);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeletePanel(panel.Id) as RedirectToActionResult;

            // Assert
            result.Should().NotBeNull();
            _controller.TempData["Success"].Should().Be("Panel deleted successfully.");

            var deleted = await _context.Panels.FindAsync(panel.Id);
            deleted.Should().BeNull();
        }

        [Test]
        public async Task DeletePanel_WhenScheduled_ShouldReturnError()
        {
            // Arrange
            var panel = TestDataFactory.CreatePanel(1, "Scheduled Panel");
            await _context.Panels.AddAsync(panel);

            var schedule = TestDataFactory.CreateDefenseSchedule(1, panel.Id);
            await _context.DefenseSchedules.AddAsync(schedule);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeletePanel(panel.Id) as RedirectToActionResult;

            // Assert
            result.Should().NotBeNull();
            _controller.TempData["Error"].ToString().Should().Contain("currently assigned to a defense schedule");

            var stillExists = await _context.Panels.FindAsync(panel.Id);
            stillExists.Should().NotBeNull();
        }

        #endregion

        #region Schedule Defense Tests

        [Test]
        public async Task ScheduleDefense_Get_ShouldReturnSchedulesWithProjectsAndPanels()
        {
            // Arrange
            var project = TestDataFactory.CreateProject(1);
            var panel = TestDataFactory.CreatePanel(1, "Panel A", true);
            await _context.Projects.AddAsync(project);
            await _context.Panels.AddAsync(panel);

            var schedule = TestDataFactory.CreateDefenseSchedule(project.Id, panel.Id);
            await _context.DefenseSchedules.AddAsync(schedule);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ScheduleDefense() as ViewResult;

            // Assert
            result.Should().NotBeNull();
            var model = result.Model as List<DefenseSchedule>;
            model.Should().HaveCount(1);
        }

        [Test]
        public async Task CreateSchedule_PanelNotApproved_ShouldReturnError()
        {
            // Arrange
            var panel = TestDataFactory.CreatePanel(1, "Unapproved Panel", false);
            await _context.Panels.AddAsync(panel);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.CreateSchedule(1, panel.Id, "Proposal Defense", DateTime.Now.AddDays(7), "Room 101") as RedirectToActionResult;

            // Assert
            result.Should().NotBeNull();
            _controller.TempData["Error"].ToString().Should().Contain("not been approved by the HOD");
        }

        [Test]
        public async Task CreateSchedule_RoomConflict_ShouldReturnError()
        {
            // Arrange
            var panel = TestDataFactory.CreatePanel(1, "Approved Panel", true);
            await _context.Panels.AddAsync(panel);

            var existingSchedule = new DefenseSchedule
            {
                ProjectId = 1,
                PanelId = 1,
                DefenseType = "Proposal Defense",
                Date = new DateTime(2024, 12, 25, 10, 0, 0),
                Room = "Room 101"
            };
            await _context.DefenseSchedules.AddAsync(existingSchedule);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.CreateSchedule(2, panel.Id, "Proposal Defense", new DateTime(2024, 12, 25, 10, 0, 0), "Room 101") as RedirectToActionResult;

            // Assert
            result.Should().NotBeNull();
            _controller.TempData["Error"].ToString().Should().Contain("Room 'Room 101' is already booked");
        }

        [Test]
        public async Task CreateSchedule_ValidData_ShouldCreateSchedule()
        {
            // Arrange
            var project = TestDataFactory.CreateProject(1);
            var panel = TestDataFactory.CreatePanel(1, "Approved Panel", true);
            await _context.Projects.AddAsync(project);
            await _context.Panels.AddAsync(panel);
            await _context.SaveChangesAsync();

            var defenseDate = DateTime.Now.AddDays(7);

            // Act
            var result = await _controller.CreateSchedule(project.Id, panel.Id, "Proposal Defense", defenseDate, "Room 202") as RedirectToActionResult;

            // Assert
            result.Should().NotBeNull();
            _controller.TempData["Success"].ToString().Should().Contain("scheduled successfully");

            var schedule = await _context.DefenseSchedules.FirstOrDefaultAsync();
            schedule.Should().NotBeNull();
            schedule.ProjectId.Should().Be(project.Id);
            schedule.PanelId.Should().Be(panel.Id);
            schedule.DefenseType.Should().Be("Proposal Defense");
        }

        #endregion

        #region Manage Deadlines Tests

        [Test]
        public async Task ManageDeadlines_Get_WhenNoSettings_ShouldCreateDefault()
        {
            // Act
            var result = await _controller.ManageDeadlines() as ViewResult;

            // Assert
            result.Should().NotBeNull();
            var model = result.Model as GlobalSettings;
            model.Should().NotBeNull();
            model.RegistrationOpen.Should().BeTrue();

            var saved = await _context.GlobalSettings.FirstOrDefaultAsync();
            saved.Should().NotBeNull();
        }

        [Test]
        public async Task UpdateDeadlines_ShouldUpdateSettings()
        {
            // Arrange
            var settings = TestDataFactory.CreateActiveGlobalSettings();
            await _context.GlobalSettings.AddAsync(settings);
            await _context.SaveChangesAsync();

            var updatedSettings = new GlobalSettings
            {
                RegistrationOpen = false,
                RegistrationDeadline = DateTime.Now.AddDays(15)
            };

            // Act
            var result = await _controller.UpdateDeadlines(updatedSettings) as RedirectToActionResult;

            // Assert
            result.Should().NotBeNull();
            _controller.TempData["Success"].Should().Be("All system deadlines have been updated.");

            var saved = await _context.GlobalSettings.FirstOrDefaultAsync();
            saved.RegistrationOpen.Should().BeFalse();
        }

        #endregion

        #region Edit User Tests

        [Test]
        public async Task EditUser_Get_ValidId_ShouldReturnViewWithModel()
        {
            // Arrange
            var user = TestDataFactory.CreateStudent("edit@test.com", "Edit User");
            _userManagerMock.Setup(um => um.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.EditUser(user.Id) as ViewResult;

            // Assert
            result.Should().NotBeNull();
            var model = result.Model as UserEditViewModel;
            model.Should().NotBeNull();
            model.Id.Should().Be(user.Id);
            model.FullName.Should().Be(user.FullName);
            model.Email.Should().Be(user.Email);
        }

        [Test]
        public async Task EditUser_Post_ValidModel_ShouldUpdateUser()
        {
            // Arrange
            var model = new UserEditViewModel
            {
                Id = "user-123",
                FullName = "Updated Name",
                Email = "updated@test.edu"
            };

            var user = TestDataFactory.CreateStudent("old@test.com", "Old Name");
            _userManagerMock.Setup(um => um.FindByIdAsync(model.Id))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.EditUser(model) as RedirectToActionResult;

            // Assert
            result.Should().NotBeNull();
            result.ActionName.Should().Be("ManageUsers");
            _controller.TempData["Success"].Should().Be("User details updated successfully.");
        }

        #endregion

        #region Create User Tests

        [Test]
        public async Task CreateUser_ValidData_ShouldCreateAccount()
        {
            // Arrange
            _userManagerMock.Setup(um => um.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser)null);
            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.CreateUser("New Supervisor", "newsup@test.edu", "Supervisor", "Pass123!", null) as RedirectToActionResult;

            // Assert
            result.Should().NotBeNull();
            _controller.TempData["Success"].ToString().Should().Contain("Account created");
        }

        [Test]
        public async Task CreateUser_DuplicateEmail_ShouldReturnError()
        {
            // Arrange
            var existingUser = TestDataFactory.CreateSupervisor();
            _userManagerMock.Setup(um => um.FindByEmailAsync(existingUser.Email))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _controller.CreateUser("Test", existingUser.Email, "Supervisor", "Pass123!", null) as ViewResult;

            // Assert
            result.Should().NotBeNull();
            _controller.TempData["Error"].Should().Be("This email address is already in use.");
        }

        [Test]
        public async Task CreateUser_PanelMember_ShouldAddClaim()
        {
            // Arrange
            _userManagerMock.Setup(um => um.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser)null);
            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Panel"))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(um => um.AddClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.CreateUser("Panel Member", "panel@test.edu", "Panel", "Pass123!", "Internal") as RedirectToActionResult;

            // Assert
            result.Should().NotBeNull();
            _userManagerMock.Verify(um => um.AddClaimAsync(It.IsAny<ApplicationUser>(), It.Is<Claim>(c => c.Type == "PanelistType" && c.Value == "Internal")), Times.Once);
        }

        #endregion
    }
}