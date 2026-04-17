using FYP_App.Models;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace FYP_App.Tests.Helpers
{
    public static class TestDataFactory
    {
        #region FypRegistration
        public static FypRegistration CreateValidFypRegistration(int id = 0)
        {
            return new FypRegistration
            {
                Id = id,
                Student1Name = "John Doe",
                Student1Email = "john.doe@student.edu",
                Student1RegNo = "01-123456-001",
                Student2Name = "Jane Smith",
                Student2Email = "jane.smith@student.edu",
                Student2RegNo = "01-123456-002",
                ProposedTitle = "AI-Powered Document Management System",
                ProposedDomain = "Artificial Intelligence / ML",
                PreferredSupervisor = "Dr. Robert Johnson",
                RegisteredAt = DateTime.Now,
                IsProcessed = false
            };
        }

        public static FypRegistration CreateDuplicateEmailRegistration()
        {
            return new FypRegistration
            {
                Id = 2,
                Student1Name = "Alice Brown",
                Student1Email = "john.doe@student.edu",
                Student1RegNo = "01-123456-003",
                Student2Name = "Bob Wilson",
                Student2Email = "bob.wilson@student.edu",
                Student2RegNo = "01-123456-004",
                ProposedTitle = "Blockchain Voting System",
                ProposedDomain = "Blockchain",
                PreferredSupervisor = "Dr. Sarah Lee",
                RegisteredAt = DateTime.Now,
                IsProcessed = false
            };
        }
        #endregion

        #region GlobalSettings
        public static GlobalSettings CreateActiveGlobalSettings()
        {
            return new GlobalSettings
            {
                Id = 1,
                RegistrationOpen = true,
                RegistrationDeadline = DateTime.Now.AddDays(30),
                ProposalDeadline = DateTime.Now.AddDays(45),
                SRSDeadline = DateTime.Now.AddDays(60),
                SDSDeadline = DateTime.Now.AddDays(75),
                MeetingLogDeadline = DateTime.Now.AddDays(90),
                FinalReportDeadline = DateTime.Now.AddDays(105),
                WarningThresholdDays = 3
            };
        }

        public static GlobalSettings CreateClosedGlobalSettings()
        {
            return new GlobalSettings
            {
                Id = 1,
                RegistrationOpen = false,
                RegistrationDeadline = DateTime.Now.AddDays(-1)
            };
        }
        #endregion

        #region ApplicationUser
        public static ApplicationUser CreateApplicationUser(string email, string fullName)
        {
            return new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = email,
                Email = email,
                FullName = fullName
            };
        }

        public static ApplicationUser CreateStudent(string email = "student@test.edu", string name = "Test Student")
        {
            return CreateApplicationUser(email, name);
        }

        public static ApplicationUser CreateSupervisor(string email = "supervisor@test.edu", string name = "Dr. Test Supervisor")
        {
            return CreateApplicationUser(email, name);
        }

        public static ApplicationUser CreatePanelMember(string email = "panel@test.edu", string name = "Panel Member")
        {
            return CreateApplicationUser(email, name);
        }
        #endregion

        #region Project
        public static Project CreateProject(int id = 1, string studentId = null, string supervisorId = null)
        {
            return new Project
            {
                Id = id,
                Title = "Test Project Title",
                Description = "Test Project Description",
                Status = "Active",
                StudentId = studentId ?? Guid.NewGuid().ToString(),
                SupervisorId = supervisorId,
                Student1RegNo = "01-123456-001",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                SupervisorFlagged = false,
                WarningMessage = string.Empty
            };
        }
        #endregion

        #region Panel
        public static Panel CreatePanel(int id = 1, string name = "Test Panel", bool hodApproval = false)
        {
            return new Panel
            {
                Id = id,
                Name = name,
                HODApproval = hodApproval,
                Members = new List<PanelMember>()
            };
        }
        #endregion

        #region PanelMember
        public static PanelMember CreatePanelMember(int panelId, string userId, string role = "Member")
        {
            return new PanelMember
            {
                Id = new Random().Next(1, 9999),
                PanelId = panelId,
                UserId = userId,
                Role = role
            };
        }
        #endregion

        #region DefenseSchedule
        public static DefenseSchedule CreateDefenseSchedule(int projectId, int panelId, string defenseType = "Proposal Defense")
        {
            return new DefenseSchedule
            {
                Id = new Random().Next(1, 9999),
                ProjectId = projectId,
                PanelId = panelId,
                DefenseType = defenseType,
                Date = DateTime.Now.AddDays(7),
                Room = "Room 101",
                NotificationSent = false
            };
        }
        #endregion

        #region Submission
        public static Submission CreateSubmission(int projectId, string type = "Proposal", string status = "Pending Supervisor")
        {
            return new Submission
            {
                Id = new Random().Next(1, 9999),
                ProjectId = projectId,
                SubmissionType = type,
                FilePath = $"/uploads/submissions/test_{Guid.NewGuid()}.pdf",
                Status = status,
                Remarks = null,
                SubmittedAt = DateTime.Now
            };
        }
        #endregion

        #region MeetingLog
        public static MeetingLog CreateMeetingLog(int projectId, int meetingNumber = 1, string status = "Pending")
        {
            return new MeetingLog
            {
                Id = new Random().Next(1, 9999),
                ProjectId = projectId,
                MeetingNumber = meetingNumber,
                MeetingDate = DateTime.Now,
                StudentActivities = "Discussed project requirements and timeline.",
                SupervisorComments = null,
                NextMeetingPlan = "Complete literature review by next meeting.",
                Status = status,
                Phase = meetingNumber <= 12 ? "Initial" : (meetingNumber <= 18 ? "Midterm" : "Final")
            };
        }
        #endregion

        #region ProjectGrade
        public static ProjectGrade CreateProjectGrade(int projectId)
        {
            return new ProjectGrade
            {
                Id = new Random().Next(1, 9999),
                ProjectId = projectId,
                InitialDefenseMarks = null,
                MidtermDefenseMarks = null,
                SupervisorMarks = null,
                CoordinatorMarks = null,
                FinalInternalMarks = null,
                FinalExternalMarks = null,
                Grade = "IP",
                TotalMarks = 0
            };
        }
        #endregion

        #region DefenseEvaluation
        public static DefenseEvaluation CreateDefenseEvaluation(int projectId, string evaluatorId, string defenseType, double marks = 85)
        {
            return new DefenseEvaluation
            {
                Id = new Random().Next(1, 9999),
                ProjectId = projectId,
                EvaluatorId = evaluatorId,
                DefenseType = defenseType,
                Marks = marks,
                Feedback = "Good presentation and technical depth."
            };
        }
        #endregion

        #region DocumentTemplate
        public static DocumentTemplate CreateDocumentTemplate(string title = "Test Template")
        {
            return new DocumentTemplate
            {
                Id = new Random().Next(1, 9999),
                Title = title,
                Description = "Test template description",
                FilePath = "/uploads/templates/test_template.pdf",
                FileName = "test_template.pdf",
                UploadedAt = DateTime.Now
            };
        }
        #endregion

        #region IFormFile Helper
        public static IFormFile CreateTestFormFile(string content = "test content", string fileName = "test.pdf")
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, bytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };
        }
        #endregion

        #region SystemAlertViewModel
        public static SystemAlertViewModel CreateSystemAlert(string type = "Student")
        {
            return new SystemAlertViewModel
            {
                Type = type,
                ProjectTitle = "Test Project",
                PersonName = "Test Person",
                UserId = Guid.NewGuid().ToString(),
                ProjectId = 1,
                Issue = "Missed deadlines",
                DaysInactive = 15
            };
        }
        #endregion
    }
}