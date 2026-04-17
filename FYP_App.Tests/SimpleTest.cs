using FYP_App.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FYP_App.Tests
{
    [TestFixture]
    public class SimpleTest
    {
        [Test]
        public void Test1_TestDataFactory_CreatesValidRegistration()
        {
            // Act
            var registration = TestDataFactory.CreateValidFypRegistration();

            // Assert
            Assert.That(registration, Is.Not.Null);
            Assert.That(registration.Student1Name, Is.EqualTo("John Doe"));
            Assert.That(registration.Student1Email, Is.EqualTo("john.doe@student.edu"));
            Assert.That(registration.Student1RegNo, Is.EqualTo("01-123456-001"));
            Assert.That(registration.Student2Name, Is.EqualTo("Jane Smith"));
            Assert.That(registration.ProposedTitle, Is.EqualTo("AI-Powered Document Management System"));
            Assert.That(registration.IsProcessed, Is.False);
        }

        [Test]
        public void Test2_InMemoryDatabase_Works()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
                .Options;

            using var context = new ApplicationDbContext(options);

            // Act
            var settings = TestDataFactory.CreateActiveGlobalSettings();
            context.GlobalSettings.Add(settings);
            context.SaveChanges();

            // Assert
            var saved = context.GlobalSettings.FirstOrDefault();
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved.RegistrationOpen, Is.True);
        }

        [Test]
        public void Test3_ApplicationUser_Creation_Works()
        {
            // Act
            var user = TestDataFactory.CreateStudent("test@student.edu", "Test Student");

            // Assert
            Assert.That(user, Is.Not.Null);
            Assert.That(user.Email, Is.EqualTo("test@student.edu"));
            Assert.That(user.UserName, Is.EqualTo("test@student.edu"));
            Assert.That(user.FullName, Is.EqualTo("Test Student"));
        }

        [Test]
        public void Test4_Project_Creation_Works()
        {
            // Act
            var project = TestDataFactory.CreateProject(1, "student123", "supervisor456");

            // Assert
            Assert.That(project, Is.Not.Null);
            Assert.That(project.Id, Is.EqualTo(1));
            Assert.That(project.Title, Is.EqualTo("Test Project Title"));
            Assert.That(project.Status, Is.EqualTo("Active"));
        }

        [Test]
        public void Test5_Panel_Creation_Works()
        {
            // Act
            var panel = TestDataFactory.CreatePanel(1, "Defense Panel", false);

            // Assert
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.Id, Is.EqualTo(1));
            Assert.That(panel.Name, Is.EqualTo("Defense Panel"));
            Assert.That(panel.HODApproval, Is.False);
        }
    }
}