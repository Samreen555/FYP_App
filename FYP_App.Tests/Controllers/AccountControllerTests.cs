using FYP_App.Controllers;
using FYP_App.Data;
using FYP_App.Models;
using FYP_App.Tests.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FYP_App.Tests.Controllers
{
    [TestFixture]
    public class AccountControllerTests
    {
        private ApplicationDbContext _context;
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private AccountController _controller;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "AccountTestDb_" + Guid.NewGuid())
                .Options;
            _context = new ApplicationDbContext(options);

            // Setup UserManager Mock
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            // Setup SignInManager Mock
            _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
                _userManagerMock.Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
                Mock.Of<IAuthenticationSchemeProvider>(),
                Mock.Of<IUserConfirmation<ApplicationUser>>());

            _controller = new AccountController(
                _signInManagerMock.Object,
                _userManagerMock.Object,
                _context);

            // Setup TempData
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());

            // Setup ControllerContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Database?.EnsureDeleted();
            _context?.Dispose();
        }

        #region RegisterGroup Tests

        [Test]
        public async Task RegisterGroup_Get_WhenRegistrationOpen_ShouldReturnView()
        {
            // Arrange
            var settings = TestDataFactory.CreateActiveGlobalSettings();
            await _context.GlobalSettings.AddAsync(settings);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.RegisterGroup() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.ViewBag.IsActive, Is.True);
        }

        [Test]
        public async Task RegisterGroup_Get_WhenRegistrationClosed_ShouldReturnViewWithActiveFalse()
        {
            // Arrange
            var settings = TestDataFactory.CreateClosedGlobalSettings();
            await _context.GlobalSettings.AddAsync(settings);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.RegisterGroup() as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.ViewBag.IsActive, Is.False);
        }

        [Test]
        public async Task RegisterGroup_Post_ValidRegistration_ShouldSaveAndReturnSuccess()
        {
            // Arrange
            var settings = TestDataFactory.CreateActiveGlobalSettings();
            await _context.GlobalSettings.AddAsync(settings);
            await _context.SaveChangesAsync();

            var registration = TestDataFactory.CreateValidFypRegistration();

            // Act
            var result = await _controller.RegisterGroup(registration) as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("RegistrationSuccess"));

            var saved = await _context.FypRegistrations.FirstOrDefaultAsync();
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved.Student1Email, Is.EqualTo(registration.Student1Email));
        }

        [Test]
        public async Task RegisterGroup_Post_DuplicateEmail_ShouldReturnModelError()
        {
            // Arrange
            var settings = TestDataFactory.CreateActiveGlobalSettings();
            await _context.GlobalSettings.AddAsync(settings);

            var existing = TestDataFactory.CreateValidFypRegistration();
            await _context.FypRegistrations.AddAsync(existing);
            await _context.SaveChangesAsync();

            var duplicate = TestDataFactory.CreateDuplicateEmailRegistration();

            // Act
            var result = await _controller.RegisterGroup(duplicate) as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.ModelState.IsValid, Is.False);

            var modelStateEntry = _controller.ModelState[string.Empty];
            Assert.That(modelStateEntry, Is.Not.Null);
            Assert.That(modelStateEntry.Errors[0].ErrorMessage, Does.Contain("already registered"));
        }

        [Test]
        public async Task RegisterGroup_Post_WhenRegistrationClosed_ShouldReturnErrorContent()
        {
            // Arrange
            var settings = TestDataFactory.CreateClosedGlobalSettings();
            await _context.GlobalSettings.AddAsync(settings);
            await _context.SaveChangesAsync();

            var registration = TestDataFactory.CreateValidFypRegistration();

            // Act
            var result = await _controller.RegisterGroup(registration) as ContentResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Content, Is.EqualTo("Error: Registration is currently closed."));
        }

        #endregion

        #region Login Tests

        [Test]
        public async Task Login_Post_ValidCredentials_ShouldRedirectToDashboard()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = "student@test.edu",
                Password = "Password123!",
                Role = "Student",
                RememberMe = false
            };

            var user = TestDataFactory.CreateStudent(model.Email, "Test Student");

            _userManagerMock.Setup(um => um.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.IsInRoleAsync(user, model.Role))
                .ReturnsAsync(true);
            _signInManagerMock.Setup(sm => sm.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Act
            var result = await _controller.Login(model) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ActionName, Is.EqualTo("Index"));
            Assert.That(result.ControllerName, Is.EqualTo("Student"));
        }

        [Test]
        public async Task Login_Post_InvalidRole_ShouldReturnModelError()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = "supervisor@test.edu",
                Password = "Password123!",
                Role = "Student",
                RememberMe = false
            };

            var user = TestDataFactory.CreateSupervisor(model.Email, "Dr. Supervisor");

            _userManagerMock.Setup(um => um.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.IsInRoleAsync(user, model.Role))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Login(model) as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.ModelState.IsValid, Is.False);

            var modelStateEntry = _controller.ModelState[string.Empty];
            Assert.That(modelStateEntry, Is.Not.Null);
            Assert.That(modelStateEntry.Errors[0].ErrorMessage, Does.Contain("Access Denied"));
        }

        [Test]
        public async Task Login_Post_InvalidPassword_ShouldReturnModelError()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = "student@test.edu",
                Password = "WrongPassword",
                Role = "Student",
                RememberMe = false
            };

            var user = TestDataFactory.CreateStudent(model.Email, "Test Student");

            _userManagerMock.Setup(um => um.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.IsInRoleAsync(user, model.Role))
                .ReturnsAsync(true);
            _signInManagerMock.Setup(sm => sm.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _controller.Login(model) as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(_controller.ModelState.IsValid, Is.False);
        }

        [Test]
        public async Task Logout_ShouldSignOutAndRedirectToLogin()
        {
            // Arrange
            _signInManagerMock.Setup(sm => sm.SignOutAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Logout() as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ActionName, Is.EqualTo("Login"));
            Assert.That(result.ControllerName, Is.EqualTo("Account"));
            _signInManagerMock.Verify(sm => sm.SignOutAsync(), Times.Once);
        }

        #endregion
    }
}