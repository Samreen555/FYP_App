using FYP_App.Data;
using FYP_App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FYP_App.Controllers
{
    [Authorize(Roles = "Coordinator")]
    public class CoordinatorController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IWebHostEnvironment _env;

        public CoordinatorController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _env = env;
        }

        // =============================
        // 1. DASHBOARD 
        // ===========================
        public async Task<IActionResult> Index()
        {
            ViewBag.StudentCount = (await _userManager.GetUsersInRoleAsync("Student")).Count;
            ViewBag.SupervisorCount = (await _userManager.GetUsersInRoleAsync("Supervisor")).Count;
            ViewBag.PendingRegistrations = await _context.FypRegistrations.CountAsync(r => !r.IsProcessed);

            // Count reported supervisors
            ViewBag.ReportedSupervisors = await _context.Projects.CountAsync(p => p.SupervisorFlagged == true);
            ViewBag.TotalProjects = await _context.Projects.CountAsync();

            return View();
        }

        // ==========================================
        // 2. REGISTRATION APPROVAL
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> PendingRegistrations()
        {
            var pending = await _context.FypRegistrations
                .Where(r => !r.IsProcessed)
                .OrderByDescending(r => r.RegisteredAt)
                .ToListAsync();
            return View(pending);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveRegistration(int id)
        {
            var reg = await _context.FypRegistrations.FindAsync(id);
            if (reg == null) return NotFound();

            string email = reg.Student1Email;
            string password = "FYP" + new Random().Next(1000, 9999) + "!";

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = reg.Student1Name,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Student");

                var project = new Project
                {
                    Title = reg.ProposedTitle,
                    StudentId = user.Id,
                    Status = "Approved",
                    Student1RegNo = reg.Student1RegNo,
                    Description = $"Domain: {reg.ProposedDomain}. Partner: {reg.Student2Name} ({reg.Student2RegNo})",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Projects.Add(project);

                reg.IsProcessed = true;
                _context.Update(reg);
                await _context.SaveChangesAsync();

                TempData["NewEmail"] = email;
                TempData["NewPassword"] = password;
                TempData["NewName"] = reg.Student1Name;

                return RedirectToAction("ShowCredentials");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["Error"] = $"Failed: {errors}";
                return RedirectToAction("PendingRegistrations");
            }
        }

        [HttpGet]
        public IActionResult ShowCredentials()
        {
            if (TempData["NewEmail"] == null) return RedirectToAction("Index");
            return View();
        }

        // ==========================================
        // 3. MANAGE DEFENSE PANELS (GET) 
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ManagePanels()
        {
            var panels = await _context.Panels
                .Include(p => p.Members).ThenInclude(pm => pm.User)
                .ToListAsync();

            var allUsers = await _userManager.Users.ToListAsync();
            var facultyList = new List<dynamic>();

            foreach (var user in allUsers)
            {
                // FILTER: ONLY allow users in the 'Panel' role
                bool isPanel = await _userManager.IsInRoleAsync(user, "Panel");

                if (isPanel)
                {
                    string category = "Panel Member";
                    string badgeColor = "bg-primary"; // Default Blue

                    // Check for Internal/External Claim
                    var claims = await _userManager.GetClaimsAsync(user);
                    var type = claims.FirstOrDefault(c => c.Type == "PanelistType")?.Value;

                    if (type == "Internal")
                    {
                        category = "Internal Examiner";
                        badgeColor = "bg-info text-dark";
                    }
                    else if (type == "External")
                    {
                        category = "External Examiner";
                        badgeColor = "bg-danger"; 
                    }

                    facultyList.Add(new
                    {
                        Id = user.Id,
                        Name = user.FullName,
                        Email = user.Email,
                        Category = category,
                        Badge = badgeColor
                    });
                }
            }

            ViewBag.FacultyList = facultyList.OrderBy(f => f.Category).ToList();
            return View(panels);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePanel(string Name, List<string> MemberIds)
        {
            if (string.IsNullOrEmpty(Name) || MemberIds == null || !MemberIds.Any())
            {
                TempData["Error"] = "Panel Name and at least one member are required.";
                return RedirectToAction("ManagePanels");
            }

            var panel = new Panel { Name = Name, HODApproval = false };
            _context.Panels.Add(panel);
            await _context.SaveChangesAsync();

            foreach (var userId in MemberIds)
            {
                // Fetch the User to check their Claim (Internal/External)
                var user = await _userManager.FindByIdAsync(userId);
                var claims = await _userManager.GetClaimsAsync(user);

                // Default to "Member" if no claim exists
                var role = claims.FirstOrDefault(c => c.Type == "PanelistType")?.Value ?? "Member";

                _context.PanelMembers.Add(new PanelMember
                {
                    PanelId = panel.Id,
                    UserId = userId,
                    Role = role
                });
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = "Panel Created Successfully.";
            return RedirectToAction("ManagePanels");
        }

        // =============================
        // 4. SCHEDULE DEFENSES
        // ============================
        [HttpGet]
        public async Task<IActionResult> ScheduleDefense()
        {
            ViewBag.Projects = await _context.Projects.Include(p => p.Student).ToListAsync();

            // Only fetch Approved Panels
            ViewBag.Panels = await _context.Panels
                .Where(p => p.HODApproval == true) 
                .ToListAsync();

            var schedules = await _context.DefenseSchedules
                .Include(d => d.Project)
                .Include(d => d.Panel)
                .OrderBy(d => d.Date)
                .ToListAsync();

            return View(schedules);
        }

        // =================================
        // CREATE SCHEDULE (POST)
        // ==================================
        [HttpPost]
        public async Task<IActionResult> CreateSchedule(int ProjectId, int PanelId, string DefenseType, DateTime Date, string Room)
        {
            // 1. Verify Panel Approval
            var panel = await _context.Panels.FindAsync(PanelId);
            if (panel == null || !panel.HODApproval)
            {
                TempData["Error"] = "Action Denied: This panel has not been approved by the HOD yet.";
                return RedirectToAction("ScheduleDefense");
            }

            // 2. Check for Duplicates
            var exists = await _context.DefenseSchedules
                .AnyAsync(d => d.ProjectId == ProjectId && d.DefenseType == DefenseType);
            if (exists)
            {
                TempData["Error"] = $"This project already has a schedule for {DefenseType}.";
                return RedirectToAction("ScheduleDefense");
            }

            // 3. Sequence validation
            var sequence = new List<string> { "Proposal Defense", "Initial Defense", "Midterm Defense", "Final Defense" };
            int currentIndex = sequence.IndexOf(DefenseType);

            // If it's not the first one (Proposal), check the previous step
            if (currentIndex > 0)
            {
                string prevType = sequence[currentIndex - 1];

                var prevDefense = await _context.DefenseSchedules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.ProjectId == ProjectId && d.DefenseType == prevType);

                // Rule A: Previous defense must exist
                if (prevDefense == null)
                {
                    TempData["Error"] = $"Sequence Error: You must schedule '{prevType}' before scheduling '{DefenseType}'.";
                    return RedirectToAction("ScheduleDefense");
                }

                // Rule B: Date must be AFTER previous defense
                if (Date <= prevDefense.Date)
                {
                    TempData["Error"] = $"Date Error: {DefenseType} must be scheduled AFTER {prevType} ({prevDefense.Date:dd MMM yyyy}).";
                    return RedirectToAction("ScheduleDefense");
                }
            }
            

            var schedule = new DefenseSchedule
            {
                ProjectId = ProjectId,
                PanelId = PanelId,
                DefenseType = DefenseType,
                Date = Date,
                Room = Room
            };
            _context.DefenseSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{DefenseType} scheduled successfully!";
            return RedirectToAction("ScheduleDefense");
        }

        // =============================
        // 5. MANAGE DEADLINES
        // ============================
        [HttpGet]
        public async Task<IActionResult> ManageDeadlines()
        {
            var settings = await _context.GlobalSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new GlobalSettings { RegistrationDeadline = DateTime.Now.AddDays(7), RegistrationOpen = true };
                _context.GlobalSettings.Add(settings);
                await _context.SaveChangesAsync();
            }
            return View(settings);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDeadlines(GlobalSettings model)
        {
            var settings = await _context.GlobalSettings.FirstOrDefaultAsync();
            if (settings != null)
            {
                settings.RegistrationOpen = model.RegistrationOpen;
                settings.RegistrationDeadline = model.RegistrationDeadline;
                settings.ProposalDeadline = model.ProposalDeadline;
                settings.SRSDeadline = model.SRSDeadline;
                settings.SDSDeadline = model.SDSDeadline;
                settings.FinalReportDeadline = model.FinalReportDeadline;
                

                _context.Update(settings);
                await _context.SaveChangesAsync();
                TempData["Success"] = "All system deadlines have been updated.";
            }
            return RedirectToAction("ManageDeadlines");
        }

        // =============================
        // 6. MANAGE PROJECTS
        // ============================
        [HttpGet]
        public async Task<IActionResult> ManageProjects()
        {
            var projects = await _context.Projects
                .Include(p => p.Student)
                .Include(p => p.Supervisor)
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.Supervisors = await _userManager.GetUsersInRoleAsync("Supervisor");
            return View(projects);
        }

        [HttpPost]
        public async Task<IActionResult> AssignSupervisor(int projectId, string supervisorId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return NotFound();

            var supervisor = await _userManager.FindByIdAsync(supervisorId);
            if (supervisor == null) { TempData["Error"] = "Supervisor not found."; return RedirectToAction("ManageProjects"); }

            project.SupervisorId = supervisor.Id;
            project.Status = "Assigned";

            _context.Update(project);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Supervisor {supervisor.FullName} assigned to {project.Title}.";
            return RedirectToAction("ManageProjects");
        }

        // ==============================
        // 7. MANAGE TEMPLATES
        // ==============================
        [HttpGet]
        public async Task<IActionResult> ManageTemplates()
        {
            var templates = await _context.DocumentTemplates.OrderByDescending(t => t.UploadedAt).ToListAsync();
            return View(templates);
        }

        [HttpPost]
        public async Task<IActionResult> UploadTemplate(string title, string description, IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "templates");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueName = Guid.NewGuid().ToString() + "_" + file.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var template = new DocumentTemplate
                {
                    Title = title,
                    Description = description,
                    FilePath = "/uploads/templates/" + uniqueName,
                    FileName = file.FileName,
                    UploadedAt = DateTime.Now
                };

                _context.DocumentTemplates.Add(template);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Template uploaded successfully.";
            }
            else { TempData["Error"] = "Please select a file."; }

            return RedirectToAction("ManageTemplates");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var template = await _context.DocumentTemplates.FindAsync(id);
            if (template != null)
            {
                string physicalPath = _webHostEnvironment.WebRootPath + template.FilePath;
                if (System.IO.File.Exists(physicalPath)) System.IO.File.Delete(physicalPath);

                _context.DocumentTemplates.Remove(template);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Template deleted.";
            }
            return RedirectToAction("ManageTemplates");
        }

        // ===========================
        // 8. MANAGE USERS
        //===========================
        [HttpGet]
        public async Task<IActionResult> ManageUsers(string role = "Supervisor")
        {
            var users = await _userManager.GetUsersInRoleAsync(role);
            ViewBag.CurrentRole = role;
            ViewBag.Roles = new List<string> { "Student", "Supervisor", "Panel", "HOD", "Coordinator" };
            return View(users);
        }

        [HttpGet]
        public IActionResult CreateUser() => View();

        [HttpPost]
        public async Task<IActionResult> CreateUser(string FullName, string Email, string Role, string Password, string PanelistType)
        {
            if (await _userManager.FindByEmailAsync(Email) != null)
            {
                TempData["Error"] = "This email address is already in use.";
                return View();
            }

            var user = new ApplicationUser
            {
                UserName = Email,
                Email = Email,
                FullName = FullName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, Role);

                
                if (Role == "Panel" && !string.IsNullOrEmpty(PanelistType))
                {
                    
                    await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("PanelistType", PanelistType));
                }
                

                TempData["Success"] = $"Account created for {FullName} ({Role}).";
                return RedirectToAction("ManageUsers", new { role = Role });
            }

            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View();
        }

        // ==========================
        // 9. SUBMISSION REVIEWS
        // =========================
        [HttpGet]
        public async Task<IActionResult> StudentSubmissions()
        {
            var submissions = await _context.Submissions
                .Include(s => s.Project).ThenInclude(p => p.Student)
                .Include(s => s.Project).ThenInclude(p => p.Supervisor)
                .Where(s => s.Status == "Pending Coordinator" || s.Status == "Approved")
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();
            return View(submissions);
        }

        [HttpPost]
        public async Task<IActionResult> ReviewSubmission(int id, string decision, string remarks)
        {
            var submission = await _context.Submissions.FindAsync(id);
            if (submission == null) return NotFound();

            if (decision == "Approve")
            {
                submission.Status = "Approved";
                submission.Remarks = "Coordinator Approved: " + (remarks ?? "No remarks");
                TempData["Success"] = "Submission finalized and approved.";
            }
            else
            {
                submission.Status = "Rejected";
                submission.Remarks = "Coordinator Rejection: " + remarks;
                TempData["Error"] = "Submission rejected.";
            }

            _context.Update(submission);
            await _context.SaveChangesAsync();
            return RedirectToAction("StudentSubmissions");
        }

        // ==========================================
        // 10. GRADING & RESULTS 
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GradingSheet(string type)
        {
            ViewBag.Type = type; // "Initial", "Midterm", or "Final"

            var grades = await _context.Projects
                .Include(p => p.Student)
                .Include(p => p.Supervisor)
                .GroupJoin(_context.ProjectGrades, p => p.Id, g => g.ProjectId, (p, g) => new { Project = p, Grade = g.FirstOrDefault() })
                .ToListAsync();

            // Fetch Schedules for Validation
            string defenseType = type + " Defense";
            var schedules = await _context.DefenseSchedules.Where(d => d.DefenseType == defenseType).ToListAsync();
            ViewBag.Schedules = schedules;

            return View(grades);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateGrades(int projectId, double marks, string type)
        {
            // DATE VALIDATION FOR FINAL DEFENSE 
            if (type == "Final")
            {
                var defense = await _context.DefenseSchedules
                    .FirstOrDefaultAsync(d => d.ProjectId == projectId && d.DefenseType == "Final Defense");

                // If no schedule OR today is not the defense date, block grading.
                if (defense == null || defense.Date.Date != DateTime.Today)
                {
                    TempData["Error"] = "Action Denied: Final Grading is only open on the day of the Defense.";
                    return RedirectToAction("GradingSheet", new { type });
                }
            }
           

            var grade = await _context.ProjectGrades.FirstOrDefaultAsync(g => g.ProjectId == projectId);

            if (grade == null)
            {
                grade = new ProjectGrade { ProjectId = projectId };
                _context.ProjectGrades.Add(grade);
            }

            // Update specific marks based on the sheet type
            if (type == "Initial") grade.InitialDefenseMarks = marks;
            else if (type == "Midterm") grade.MidtermDefenseMarks = marks;
            else if (type == "Final") grade.CoordinatorMarks = marks;

            
            double total = (grade.InitialDefenseMarks ?? 0) +
                           (grade.MidtermDefenseMarks ?? 0) +
                           (grade.SupervisorMarks ?? 0) +
                           (grade.CoordinatorMarks ?? 0) +
                           (grade.FinalInternalMarks ?? 0) +
                           (grade.FinalExternalMarks ?? 0);

            grade.TotalMarks = total;

            // Assign Grade Letter
            if (total >= 80) grade.Grade = "A";
            else if (total >= 70) grade.Grade = "B";
            else if (total >= 60) grade.Grade = "C";
            else if (total >= 50) grade.Grade = "D";
            else grade.Grade = "F";

            if (grade.InitialDefenseMarks == null || grade.MidtermDefenseMarks == null ||
                grade.SupervisorMarks == null || grade.CoordinatorMarks == null ||
                grade.FinalInternalMarks == null)
            {
                grade.Grade = "IP"; // IP: In Progress 
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Grades updated successfully.";
            return RedirectToAction("GradingSheet", new { type });
        }

        // ======================
        // 11. EDIT USER
        // =====================
        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) { TempData["Error"] = "User not found."; return RedirectToAction("ManageUsers"); }

            var model = new UserEditViewModel { Id = user.Id, FullName = user.FullName, Email = user.Email };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(UserEditViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user != null)
            {
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.UserName = model.Email;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["Success"] = "User details updated successfully.";
                    return RedirectToAction("ManageUsers");
                }
                foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            }
            else { TempData["Error"] = "User not found."; }

            return View(model);
        }

        // ===========================
        // EDIT PANEL (GET)
        // ===========================
        [HttpGet]
        public async Task<IActionResult> EditPanel(int id)
        {
            var panel = await _context.Panels
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (panel == null) return NotFound();

            var allUsers = await _userManager.Users.ToListAsync();
            var facultyList = new List<dynamic>();

            foreach (var user in allUsers)
            {
                // Only allow users in the Panel role
                bool isPanel = await _userManager.IsInRoleAsync(user, "Panel");

                if (isPanel)
                {
                    string category = "Panel Member";
                    string badgeColor = "bg-primary";

                    var claims = await _userManager.GetClaimsAsync(user);
                    var type = claims.FirstOrDefault(c => c.Type == "PanelistType")?.Value;

                    if (type == "Internal") { category = "Internal Examiner"; badgeColor = "bg-info text-dark"; }
                    else if (type == "External") { category = "External Examiner"; badgeColor = "bg-danger"; }

                    facultyList.Add(new
                    {
                        Id = user.Id,
                        Name = user.FullName,
                        Email = user.Email,
                        Category = category,
                        Badge = badgeColor,
                        IsSelected = panel.Members.Any(m => m.UserId == user.Id)
                    });
                }
            }

            ViewBag.FacultyList = facultyList.OrderBy(f => f.Category).ToList();
            return View(panel);
        }

        // ==============================
        // EDIT PANEL (POST)
        // ==============================
        [HttpPost]
        public async Task<IActionResult> EditPanel(int id, string Name, List<string> MemberIds)
        {
            var panel = await _context.Panels.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == id);
            if (panel == null) return NotFound();

            if (string.IsNullOrEmpty(Name) || MemberIds == null || !MemberIds.Any())
            {
                TempData["Error"] = "Panel Name and at least one member are required.";
                return RedirectToAction("EditPanel", new { id });
            }

            // 1. Update Name
            panel.Name = Name;

            // 2. Update Members 
            _context.PanelMembers.RemoveRange(panel.Members);

            foreach (var userId in MemberIds)
            {
                var user = await _userManager.FindByIdAsync(userId);
                var claims = await _userManager.GetClaimsAsync(user);
                var role = claims.FirstOrDefault(c => c.Type == "PanelistType")?.Value ?? "Member";

                _context.PanelMembers.Add(new PanelMember { PanelId = panel.Id, UserId = userId, Role = role });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Panel updated successfully.";
            return RedirectToAction("ManagePanels");
        }

        // ===========================
        // DELETE PANEL (POST) 
        // ===========================
        [HttpPost]
        public async Task<IActionResult> DeletePanel(int id)
        {
            // 1. Fetch Panel ALONG WITH its Members
            var panel = await _context.Panels
                .Include(p => p.Members) 
                .FirstOrDefaultAsync(p => p.Id == id);

            if (panel != null)
            {
                // 2. Check if the Panel is used in any Defense Schedule
                bool isScheduled = await _context.DefenseSchedules.AnyAsync(ds => ds.PanelId == id);
                if (isScheduled)
                {
                    TempData["Error"] = "Cannot delete this panel because it is currently assigned to a defense schedule.";
                    return RedirectToAction("ManagePanels");
                }

                // 3. REMOVE MEMBERS FIRST 
                if (panel.Members.Any())
                {
                    _context.PanelMembers.RemoveRange(panel.Members);
                }

                // 4. Now it is safe to remove the Panel
                _context.Panels.Remove(panel);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Panel deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Panel not found.";
            }

            return RedirectToAction("ManagePanels");
        }

        // ==========================
        // EDIT SCHEDULE (POST)
        // ===========================
        [HttpPost]
        public async Task<IActionResult> EditSchedule(int id, DateTime Date, string Room)
        {
            var schedule = await _context.DefenseSchedules.FindAsync(id);
            if (schedule != null)
            {
                schedule.Date = Date;
                schedule.Room = Room;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Schedule updated successfully.";
            }
            return RedirectToAction("ScheduleDefense");
        }

        // =======================
        // DELETE SCHEDULE (POST)
        //==========================
        [HttpPost]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var schedule = await _context.DefenseSchedules.FindAsync(id);
            if (schedule != null)
            {
                _context.DefenseSchedules.Remove(schedule);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Schedule removed successfully.";
            }
            return RedirectToAction("ScheduleDefense");
        }
        // ==============================
        // SUPERVISOR ALERTS (REPORTED BY HOD)
        // ==============================
        [HttpGet]
        public async Task<IActionResult> SupervisorAlerts()
        {
            
            var reportedProjects = await _context.Projects
                .Include(p => p.Supervisor)
                .Where(p => p.SupervisorFlagged == true)
                .ToListAsync();

            return View(reportedProjects);
        }

        // ===========================
        // 12. ACKNOWLEDGE REPORT 
        // ==========================
        [HttpPost]
        public async Task<IActionResult> AcknowledgeReport(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project != null)
            {
                project.SupervisorFlagged = false; // Clears the flag
                await _context.SaveChangesAsync();
                TempData["Success"] = "Report acknowledged and removed from list.";
            }
            return RedirectToAction("SupervisorAlerts");
        }
    }
}