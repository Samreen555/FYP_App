using FYP_App.Models;
using FYP_App.Tests.Helpers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FluentAssertions;

namespace FYP_App.Tests.Models
{
    [TestFixture]
    public class ModelValidationTests
    {
        #region FypRegistration Validation Tests

        [Test]
        public void FypRegistration_ValidModel_ShouldPassValidation()
        {
            // Arrange
            var registration = TestDataFactory.CreateValidFypRegistration();
            var context = new ValidationContext(registration);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(registration, context, results, true);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Test]
        public void FypRegistration_MissingRequiredFields_ShouldFailValidation()
        {
            // Arrange
            var registration = new FypRegistration();
            var context = new ValidationContext(registration);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(registration, context, results, true);

            // Assert
            isValid.Should().BeFalse();
            results.Should().Contain(r => r.MemberNames.Contains("Student1Name"));
            results.Should().Contain(r => r.MemberNames.Contains("Student1Email"));
            results.Should().Contain(r => r.MemberNames.Contains("Student1RegNo"));
            results.Should().Contain(r => r.MemberNames.Contains("Student2Name"));
            results.Should().Contain(r => r.MemberNames.Contains("Student2Email"));
            results.Should().Contain(r => r.MemberNames.Contains("Student2RegNo"));
            results.Should().Contain(r => r.MemberNames.Contains("ProposedTitle"));
            results.Should().Contain(r => r.MemberNames.Contains("ProposedDomain"));
            results.Should().Contain(r => r.MemberNames.Contains("PreferredSupervisor"));
        }

        [Test]
        [TestCase("invalid-email")]
        [TestCase("notanemail")]
        [TestCase("@missingusername.com")]
        [TestCase("test@")]
        [TestCase("@test.com")]
        public void FypRegistration_InvalidEmailFormat_ShouldFailValidation(string invalidEmail)
        {
            // Arrange
            var registration = TestDataFactory.CreateValidFypRegistration();
            registration.Student1Email = invalidEmail;
            var context = new ValidationContext(registration);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(registration, context, results, true);

            // Assert
            isValid.Should().BeFalse();
            results.Should().Contain(r => r.MemberNames.Contains("Student1Email"));
        }

        #endregion

        #region Project Validation Tests

        [Test]
        public void Project_ValidModel_ShouldPassValidation()
        {
            // Arrange
            var project = new Project
            {
                Title = "Test Project",
                Description = "Test Description",
                Status = "Active"
            };
            var context = new ValidationContext(project);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(project, context, results, true);

            // Assert
            isValid.Should().BeTrue();
        }

        [Test]
        public void Project_MissingTitle_ShouldFailValidation()
        {
            // Arrange
            var project = new Project
            {
                Title = null,
                Description = "Test Description"
            };
            var context = new ValidationContext(project);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(project, context, results, true);

            // Assert
            isValid.Should().BeFalse();
            results.Should().Contain(r => r.MemberNames.Contains("Title"));
        }

        #endregion

        #region Panel Validation Tests

        [Test]
        public void Panel_ValidModel_ShouldPassValidation()
        {
            // Arrange
            var panel = new Panel
            {
                Name = "Test Panel",
                HODApproval = false
            };
            var context = new ValidationContext(panel);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(panel, context, results, true);

            // Assert
            isValid.Should().BeTrue();
        }

        [Test]
        public void Panel_MissingName_ShouldFailValidation()
        {
            // Arrange
            var panel = new Panel
            {
                Name = null
            };
            var context = new ValidationContext(panel);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(panel, context, results, true);

            // Assert
            isValid.Should().BeFalse();
            results.Should().Contain(r => r.MemberNames.Contains("Name"));
        }

        #endregion

        #region MeetingLog Validation Tests

        [Test]
        public void MeetingLog_ValidModel_ShouldPassValidation()
        {
            // Arrange
            var log = new MeetingLog
            {
                ProjectId = 1,
                MeetingNumber = 1,
                MeetingDate = DateTime.Now,
                StudentActivities = "Discussed requirements",
                NextMeetingPlan = "Complete literature review",
                Status = "Pending",
                Phase = "Initial"
            };
            var context = new ValidationContext(log);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(log, context, results, true);

            // Assert
            isValid.Should().BeTrue();
        }

        [Test]
        public void MeetingLog_MissingRequiredFields_ShouldFailValidation()
        {
            // Arrange
            var log = new MeetingLog();
            var context = new ValidationContext(log);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(log, context, results, true);

            // Assert
            isValid.Should().BeFalse();
            results.Should().Contain(r => r.MemberNames.Contains("StudentActivities"));
            results.Should().Contain(r => r.MemberNames.Contains("NextMeetingPlan"));
        }

        #endregion

        #region Submission Validation Tests

        [Test]
        public void Submission_ValidModel_ShouldPassValidation()
        {
            // Arrange
            var submission = new Submission
            {
                ProjectId = 1,
                SubmissionType = "Proposal",
                FilePath = "/uploads/test.pdf",
                Status = "Pending Supervisor"
            };
            var context = new ValidationContext(submission);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(submission, context, results, true);

            // Assert
            isValid.Should().BeTrue();
        }

        [Test]
        public void Submission_MissingRequiredFields_ShouldFailValidation()
        {
            // Arrange
            var submission = new Submission();
            var context = new ValidationContext(submission);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(submission, context, results, true);

            // Assert
            isValid.Should().BeFalse();
            results.Should().Contain(r => r.MemberNames.Contains("SubmissionType"));
            results.Should().Contain(r => r.MemberNames.Contains("FilePath"));
        }

        #endregion

        #region DocumentTemplate Validation Tests

        [Test]
        public void DocumentTemplate_ValidModel_ShouldPassValidation()
        {
            // Arrange
            var template = new DocumentTemplate
            {
                Title = "Test Template",
                Description = "Test Description",
                FilePath = "/uploads/test.pdf",
                FileName = "test.pdf"
            };
            var context = new ValidationContext(template);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(template, context, results, true);

            // Assert
            isValid.Should().BeTrue();
        }

        [Test]
        public void DocumentTemplate_MissingRequiredFields_ShouldFailValidation()
        {
            // Arrange
            var template = new DocumentTemplate();
            var context = new ValidationContext(template);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(template, context, results, true);

            // Assert
            isValid.Should().BeFalse();
            results.Should().Contain(r => r.MemberNames.Contains("Title"));
            results.Should().Contain(r => r.MemberNames.Contains("FilePath"));
        }

        #endregion

        #region UserEditViewModel Validation Tests

        [Test]
        public void UserEditViewModel_ValidModel_ShouldPassValidation()
        {
            // Arrange
            var model = new UserEditViewModel
            {
                Id = "user123",
                FullName = "Test User",
                Email = "test@test.edu"
            };
            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(model, context, results, true);

            // Assert
            isValid.Should().BeTrue();
        }

        [Test]
        public void UserEditViewModel_InvalidEmail_ShouldFailValidation()
        {
            // Arrange
            var model = new UserEditViewModel
            {
                Id = "user123",
                FullName = "Test User",
                Email = "invalid-email"
            };
            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(model, context, results, true);

            // Assert
            isValid.Should().BeFalse();
            results.Should().Contain(r => r.MemberNames.Contains("Email"));
        }

        #endregion

        #region DefenseSchedule Validation Tests

        [Test]
        public void DefenseSchedule_ValidModel_ShouldPassValidation()
        {
            // Arrange
            var schedule = new DefenseSchedule
            {
                ProjectId = 1,
                PanelId = 1,
                DefenseType = "Final Defense",
                Date = DateTime.Now.AddDays(7),
                Room = "Room 101"
            };
            var context = new ValidationContext(schedule);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(schedule, context, results, true);

            // Assert
            isValid.Should().BeTrue();
        }

        #endregion

        #region ProjectGrade Validation Tests

        [Test]
        public void ProjectGrade_GradeCalculation_ShouldWorkCorrectly()
        {
            // Arrange
            var grade = new ProjectGrade
            {
                ProjectId = 1,
                InitialDefenseMarks = 80,
                MidtermDefenseMarks = 75,
                SupervisorMarks = 85,
                CoordinatorMarks = 90,
                FinalInternalMarks = 88,
                FinalExternalMarks = 92
            };

            // Act
            var total = (grade.InitialDefenseMarks ?? 0) +
                       (grade.MidtermDefenseMarks ?? 0) +
                       (grade.SupervisorMarks ?? 0) +
                       (grade.CoordinatorMarks ?? 0) +
                       (grade.FinalInternalMarks ?? 0) +
                       (grade.FinalExternalMarks ?? 0);

            // Assert
            total.Should().Be(510);
        }

        [Test]
        public void ProjectGrade_LetterGradeCalculation_ShouldBeCorrect()
        {
            // Test cases for letter grade thresholds
            var testCases = new[]
            {
                new { Total = 85.0, Expected = "A" },
                new { Total = 75.0, Expected = "B" },
                new { Total = 65.0, Expected = "C" },
                new { Total = 55.0, Expected = "D" },
                new { Total = 45.0, Expected = "F" }
            };

            foreach (var tc in testCases)
            {
                string letterGrade;
                if (tc.Total >= 80) letterGrade = "A";
                else if (tc.Total >= 70) letterGrade = "B";
                else if (tc.Total >= 60) letterGrade = "C";
                else if (tc.Total >= 50) letterGrade = "D";
                else letterGrade = "F";

                letterGrade.Should().Be(tc.Expected);
            }
        }

        #endregion
    }
}