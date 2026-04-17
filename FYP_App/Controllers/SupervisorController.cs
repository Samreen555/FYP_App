using FYP_App.Data;
using FYP_App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FYP_App.Controllers
{
    [Authorize(Roles = "Supervisor")]
    public class SupervisorController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public SupervisorController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        private string GetUserId() => _userManager.GetUserId(User);

        // ==========================================
        // 1. DASHBOARD
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var myProjects = await _context.Projects
                .Include(p => p.Student)
                .Where(p => p.SupervisorId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var projectIds = myProjects.Select(p => p.Id).ToList();

            ViewBag.MyProjectsCount = myProjects.Count;
            ViewBag.PendingReviews = await _context.Submissions.CountAsync(s => projectIds.Contains(s.ProjectId) && s.Status == "Pending Supervisor");
            ViewBag.UpcomingDefensesCount = await _context.DefenseSchedules.CountAsync(d => projectIds.Contains(d.ProjectId) && d.Date >= DateTime.Now);
            ViewBag.SystemDeadlines = await _context.GlobalSettings.FirstOrDefaultAsync();
            ViewBag.DefenseList = await _context.DefenseSchedules.Include(d => d.Project).Where(d => projectIds.Contains(d.ProjectId) && d.Date >= DateTime.Now).OrderBy(d => d.Date).Take(5).ToListAsync();

            return View(myProjects);
        }

        // ==========================================
        // 2. MY PROJECTS
        // ==========================================
        public async Task<IActionResult> MyProjects()
        {
            var userId = GetUserId();
            var projects = await _context.Projects.Include(p => p.Student).Where(p => p.SupervisorId == userId).ToListAsync();
            return View(projects);
        }

        // ==========================================
        // 3. REVIEW DOCUMENTS (GET)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ReviewSubmissions()
        {
            var userId = GetUserId();
            var projects = await _context.Projects
                .Include(p => p.Student)
                .Include(p => p.Submissions)
                .Where(p => p.SupervisorId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }

        // ==========================================
        // 3.1 SUBMIT REVIEW / FEEDBACK (POST)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> SubmitReview(int id, string decision, string remarks)
        {
            var submission = await _context.Submissions.FindAsync(id);
            if (submission != null)
            {
                // OPTION A: Save Feedback Only 
                if (decision == "FeedbackOnly")
                {
                    submission.Remarks = remarks;

                    TempData["Success"] = "Feedback updated successfully. Student can view it now.";
                }
                // OPTION B: Make Decision 
                else
                {
                    // ---> THIS IS THE CRITICAL FIX <---
                    // Changes status to "Pending Coordinator" instead of skipping straight to "Approved"
                    submission.Status = (decision == "Approve") ? "Pending Coordinator" : "Rejected";

                    submission.Remarks = remarks;
                    TempData["Success"] = "Document status updated and forwarded to Coordinator.";
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                TempData["Error"] = "Submission not found.";
            }
            return RedirectToAction("ReviewSubmissions");
        }

        // ==========================================
        // 4. CHECK LOG BOOKS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> MeetingLogs()
        {
            var userId = GetUserId();
            var projects = await _context.Projects.Include(p => p.Student).Where(p => p.SupervisorId == userId).ToListAsync();
            var projectIds = projects.Select(p => p.Id).ToList();
            var logs = await _context.MeetingLogs.Where(m => projectIds.Contains(m.ProjectId)).ToListAsync();
            ViewBag.AllLogs = logs;
            return View(projects);
        }

        [HttpPost]
        public async Task<IActionResult> VerifyLog(int logId, string decision, string comments)
        {
            var log = await _context.MeetingLogs.FindAsync(logId);
            if (log != null)
            {
                log.Status = (decision == "Approve") ? "Verified" : "Rejected";
                log.SupervisorComments = comments;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Log #{log.MeetingNumber} updated.";
            }
            return RedirectToAction("MeetingLogs");
        }

        // ==========================================
        // 5. FINAL GRADING
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> FinalGrading()
        {
            var userId = GetUserId();
            var projects = await _context.Projects.Include(p => p.Student).GroupJoin(_context.ProjectGrades, p => p.Id, g => g.ProjectId, (p, g) => new { Project = p, Grade = g.FirstOrDefault() }).Where(x => x.Project.SupervisorId == userId).ToListAsync();
            var projectIds = projects.Select(x => x.Project.Id).ToList();
            var defenses = await _context.DefenseSchedules.Where(d => projectIds.Contains(d.ProjectId) && d.DefenseType == "Final Defense").ToListAsync();
            ViewBag.Data = projects;
            ViewBag.Defenses = defenses;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveFinalGrade(int projectId, double marks)
        {
            var defense = await _context.DefenseSchedules.FirstOrDefaultAsync(d => d.ProjectId == projectId && d.DefenseType == "Final Defense");
            if (defense == null || defense.Date.Date != DateTime.Today)
            {
                TempData["Error"] = "Grading is only allowed on the day of the Final Defense.";
                return RedirectToAction("FinalGrading");
            }
            var grade = await _context.ProjectGrades.FirstOrDefaultAsync(g => g.ProjectId == projectId);
            if (grade == null) { grade = new ProjectGrade { ProjectId = projectId }; _context.ProjectGrades.Add(grade); }
            grade.SupervisorMarks = marks;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Marks saved.";
            return RedirectToAction("FinalGrading");
        }
    }
}