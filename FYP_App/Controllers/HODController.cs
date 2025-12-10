using FYP_App.Data;
using FYP_App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FYP_App.Controllers
{
    [Authorize(Roles = "HOD")]
    public class HODController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HODController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ========================
        // 0. DASHBOARD 
        // ========================
        public async Task<IActionResult> Index()
        {
            // 1. Pending Panels: Panels created but waiting for HOD approval
            ViewBag.PendingPanels = await _context.Panels.CountAsync(p => !p.HODApproval);

            // 2. Total Projects: All projects registered in the system
            ViewBag.TotalProjects = await _context.Projects.CountAsync();

            // 3. Completed Projects: Projects that have received a Final Grade 
            ViewBag.CompletedProjects = await _context.ProjectGrades
                .CountAsync(g => g.Grade != null && g.Grade != "IP");

            return View();
        }

        // ==========================================
        // 1. APPROVE PANELS (GET)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ApprovePanels()
        {
            // Fetch only Pending Panels
            var pendingPanels = await _context.Panels
                .Include(p => p.Members).ThenInclude(m => m.User)
                .Where(p => p.HODApproval == false)
                .ToListAsync();

            return View(pendingPanels);
        }

        // ==========================================
        // 2. APPROVE ACTION (POST)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> ApprovePanel(int id)
        {
            var panel = await _context.Panels.FindAsync(id);
            if (panel != null)
            {
                panel.HODApproval = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Panel '{panel.Name}' has been approved.";
            }
            return RedirectToAction("ApprovePanels");
        }

        // ==========================================
        // 3. REJECT/DELETE ACTION (POST)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> RejectPanel(int id)
        {
            var panel = await _context.Panels.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == id);
            if (panel != null)
            {
                // Remove members first to avoid Foreign Key constraint error
                _context.PanelMembers.RemoveRange(panel.Members);
                _context.Panels.Remove(panel);
                await _context.SaveChangesAsync();
                TempData["Error"] = $"Panel '{panel.Name}' was rejected and removed.";
            }
            return RedirectToAction("ApprovePanels");
        }

        // ==========================================
        // 4. FINAL RESULTS REPORT (GET)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> FinalResults()
        {
            var results = await _context.Projects
                .Include(p => p.Student)
                .Include(p => p.Supervisor)
                .GroupJoin(
                    _context.ProjectGrades,
                    p => p.Id,
                    g => g.ProjectId,
                    (p, g) => new { Project = p, Grade = g.FirstOrDefault() }
                )
                .ToListAsync();

            ViewBag.Results = results;
            return View();
        }

        // ==========================================
        // 5. SYSTEM ALERTS (INACTIVITY MONITOR)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> SystemAlerts()
        {
            var alerts = new List<SystemAlertViewModel>();
            var today = DateTime.Today;
            var settings = await _context.GlobalSettings.FirstOrDefaultAsync();

            // 1. FETCH ACTIVE PROJECTS 
            var projects = await _context.Projects
                .Include(p => p.Student)
                .Include(p => p.MeetingLogs)
                .Include(p => p.Submissions)
                .Where(p => p.Status != "Cancelled")
                .ToListAsync();

            // 2. FETCH BLOCKED STUDENTS 
            ViewBag.BlockedStudents = await _context.Projects
                .Include(p => p.Student)
                .Where(p => p.Status == "Cancelled")
                .ToListAsync();

            // GENERATE ALERTS FOR ACTIVE STUDENTS 
            foreach (var p in projects)
            {
                var subs = p.Submissions ?? new List<Submission>();
                bool missedDeadline = false;

                if (settings != null)
                {
                    if (settings.ProposalDeadline < today && !subs.Any(s => s.SubmissionType == "Proposal")) missedDeadline = true;
                    else if (settings.SRSDeadline < today && !subs.Any(s => s.SubmissionType == "SRS Document")) missedDeadline = true;
                }

                var logs = p.MeetingLogs ?? new List<MeetingLog>();
                var lastMeeting = logs.OrderByDescending(m => m.MeetingDate).FirstOrDefault();
                int daysSinceMeeting = lastMeeting != null ? (today - lastMeeting.MeetingDate).Days : 999;

                if (missedDeadline && daysSinceMeeting > 14)
                {
                    alerts.Add(new SystemAlertViewModel
                    {
                        Type = "Student",
                        PersonName = p.Student.FullName,
                        UserId = p.StudentId,
                        ProjectTitle = p.Title,
                        ProjectId = p.Id,
                        Issue = $"Missed Deadlines & No meetings for {daysSinceMeeting} days.",
                        DaysInactive = daysSinceMeeting
                    });
                }
            }

            // GENERATE ALERTS FOR SUPERVISORS
            var supervisors = await _userManager.GetUsersInRoleAsync("Supervisor");
            foreach (var sup in supervisors)
            {
                var pendingLogs = await _context.MeetingLogs
                    .Include(m => m.Project)
                    .Where(m => m.Project.SupervisorId == sup.Id && m.Status == "Pending")
                    .ToListAsync();

                if (pendingLogs.Any())
                {
                    var oldestPending = pendingLogs.Min(m => m.MeetingDate);
                    int waitDays = (today - oldestPending).Days;

                    if (waitDays > 14)
                    {
                        alerts.Add(new SystemAlertViewModel
                        {
                            Type = "Supervisor",
                            PersonName = sup.FullName,
                            UserId = sup.Id,
                            ProjectId = pendingLogs.First().ProjectId,
                            Issue = $"Inactive: Has logs pending approval for {waitDays} days.",
                            DaysInactive = waitDays
                        });
                    }
                }
            }

            return View(alerts);
        }

        // ============================
        // ACTION: BLOCK STUDENT
        // ===========================
        [HttpPost]
        public async Task<IActionResult> BlockStudent(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project != null)
            {
                project.Status = "Cancelled";
                project.WarningMessage = "REGISTRATION CANCELLED: Consistent inactivity detected. Please contact HOD.";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Student registration has been cancelled (Blocked).";
            }
            return RedirectToAction("SystemAlerts");
        }

        // ==========================
        // ACTION: UNBLOCK STUDENT 
        // ===========================
        [HttpPost]
        public async Task<IActionResult> UnblockStudent(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project != null)
            {
                // Restore Status
                project.Status = "Active";
                // Clear the warning so they can access dashboard again
                project.WarningMessage = null;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Student account has been reactivated.";
            }
            return RedirectToAction("SystemAlerts");
        }

        // ==========================
        // ACTION: ALLOW (WARN)
        // ==========================
        [HttpPost]
        public async Task<IActionResult> AllowStudent(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project != null)
            {
                project.WarningMessage = "WARNING: You are flagged for inactivity. Please meet your supervisor immediately.";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Warning issued to student.";
            }
            return RedirectToAction("SystemAlerts");
        }

        // ==========================
        // ACTION: REPORT SUPERVISOR
        // =========================
        [HttpPost]
        public async Task<IActionResult> ReportSupervisor(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project != null)
            {
                project.SupervisorFlagged = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Supervisor has been reported to the Coordinator.";
            }
            return RedirectToAction("SystemAlerts");
        }
    }
}