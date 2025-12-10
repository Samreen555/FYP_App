using FYP_App.Data;
using FYP_App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FYP_App.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public StudentController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _context = context;
            _env = env;
        }

        private string GetUserId() => _userManager.GetUserId(User);

        // ==========================================
        // 1. STUDENT DASHBOARD
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var settings = await _context.GlobalSettings.FirstOrDefaultAsync();
            ViewBag.Settings = settings;

            var project = await _context.Projects
                .Include(p => p.Supervisor)
                .Include(p => p.Submissions)
                .FirstOrDefaultAsync(p => p.StudentId == userId);

            // Progress Logic 
            int progress = 0;
            string stage = "Registered";

            string finalGrade = null;

            if (project != null)
            {
                // 1. Check Proposal Status
                
                var proposal = project.Submissions
                    .FirstOrDefault(s => s.SubmissionType == "Proposal" && s.Status == "Approved");

                if (proposal != null)
                {
                    progress = 20;
                    stage = "Proposal Approved";
                }

               
                var grades = await _context.ProjectGrades.FirstOrDefaultAsync(g => g.ProjectId == project.Id);

                if (grades?.InitialDefenseMarks != null) { progress = 40; stage = "Initial Defense Complete"; }
                if (grades?.MidtermDefenseMarks != null) { progress = 60; stage = "Midterm Complete"; }
                if (grades?.FinalInternalMarks != null) { progress = 80; stage = "Final Defense Complete"; }

                if (grades?.Grade != "IP" && grades?.Grade != null)
                {
                    progress = 100;
                    stage = "Project Completed";
                    finalGrade = grades.Grade;
                }
            }

            ViewBag.Progress = progress;
            ViewBag.CurrentStage = stage;
            ViewBag.FinalGrade = finalGrade;

            // 3. Get All Upcoming Defenses
            if (project != null)
            {
                
                ViewBag.UpcomingDefenses = await _context.DefenseSchedules
                    .Where(d => d.ProjectId == project.Id && d.Date > DateTime.Now)
                    .OrderBy(d => d.Date)
                    .ToListAsync();
            }

            return View(project);
        }

        // ===========================
        // 2. UPLOAD CENTER 
        // ===========================
        [HttpPost]
        public async Task<IActionResult> UploadDocument(string Type, IFormFile file)
        {
            var userId = GetUserId();
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.StudentId == userId);

            if (project == null) return RedirectToAction("Index");
            if (file == null || file.Length == 0) { TempData["Error"] = "Please select a file."; return RedirectToAction("SubmissionCenter"); }

            // 1. Save File
            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "submissions");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueName = $"{project.Id}_{Type}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            string filePath = Path.Combine(uploadsFolder, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 2. Save Database Entry
            var submission = new Submission
            {
                ProjectId = project.Id,
                SubmissionType = Type,
                FilePath = "/uploads/submissions/" + uniqueName,
                Status = "Pending Supervisor", 
                SubmittedAt = DateTime.Now,
                Remarks = null 
            };

            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{Type} uploaded successfully.";
            return RedirectToAction("SubmissionCenter");
        }

        // ==========================================
        // DELETE SUBMISSION 
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> DeleteSubmission(int id)
        {
            var submission = await _context.Submissions.FindAsync(id);

            
            if (submission != null && submission.Status != "Approved")
            {
                string webRootPath = _env.WebRootPath;
                
                string relativePath = submission.FilePath.TrimStart('/').TrimStart('\\');
                string fullPath = Path.Combine(webRootPath, relativePath);

                if (System.IO.File.Exists(fullPath))
                {
                    try
                    {
                        System.IO.File.Delete(fullPath);
                    }
                    catch
                    {
                        
                    }
                }

                _context.Submissions.Remove(submission);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Submission deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Cannot delete this submission (It may already be approved or does not exist).";
            }

            return RedirectToAction("SubmissionCenter");
        }

        // ==========================================
        // 3. DIGITAL LOG BOOK
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> MeetingLogs()
        {
            var userId = GetUserId();
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.StudentId == userId);
            if (project == null) return RedirectToAction("Index");

            // Fetch existing logs
            var logs = await _context.MeetingLogs
                .Where(m => m.ProjectId == project.Id)
                .OrderBy(m => m.MeetingNumber)
                .ToListAsync();

            // Determine Phase Limits based on your requirements
            // Initial: 1-12 | Midterm: 13-18 | Final: 19-24
            int totalLogs = logs.Count;
            string currentPhase = "Initial";
            int maxLimit = 12;

            if (totalLogs >= 12 && totalLogs < 18) { currentPhase = "Midterm"; maxLimit = 18; }
            else if (totalLogs >= 18) { currentPhase = "Final"; maxLimit = 24; }

            ViewBag.CurrentPhase = currentPhase;
            ViewBag.NextMeetingNo = totalLogs + 1;
            ViewBag.CanAdd = totalLogs < 24; 

            return View(logs);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLogEntry(MeetingLog model)
        {
            var userId = GetUserId();
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.StudentId == userId);
            if (project == null) return RedirectToAction("Index");

            // VALIDATION: Check count limits
            int currentCount = await _context.MeetingLogs.CountAsync(m => m.ProjectId == project.Id);

            if (currentCount >= 24)
            {
                TempData["Error"] = "You have reached the maximum of 24 meeting logs.";
                return RedirectToAction("MeetingLogs");
            }

            model.ProjectId = project.Id;
            model.Status = "Pending";
            model.MeetingNumber = currentCount + 1; 

           
            if (model.MeetingNumber <= 12) model.Phase = "Initial";
            else if (model.MeetingNumber <= 18) model.Phase = "Midterm";
            else model.Phase = "Final";

            _context.MeetingLogs.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Meeting Log #{model.MeetingNumber} saved successfully.";
            return RedirectToAction("MeetingLogs");
        }

        // ==========================================
        // EDIT LOG (POST)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> EditLogEntry(int logId, string activities, string plan)
        {
            var log = await _context.MeetingLogs.FindAsync(logId);

            // Allow edit ONLY if status is Pending
            if (log != null && log.Status == "Pending")
            {
                log.StudentActivities = activities;
                log.NextMeetingPlan = plan;
                log.Status = "Pending"; // Re-confirm status

                await _context.SaveChangesAsync();
                TempData["Success"] = "Log entry updated successfully.";
            }
            else
            {
                TempData["Error"] = "Cannot edit this log (it may have been verified already).";
            }

            return RedirectToAction("MeetingLogs");
        }

        // ==========================================
        // DELETE LOG (POST)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> DeleteLogEntry(int logId)
        {
            var log = await _context.MeetingLogs.FindAsync(logId);

            if (log != null && log.Status == "Pending")
            {
                _context.MeetingLogs.Remove(log);
                await _context.SaveChangesAsync();

                

                TempData["Success"] = "Log entry deleted.";
            }
            else
            {
                TempData["Error"] = "Cannot delete verified logs.";
            }

            return RedirectToAction("MeetingLogs");
        }
        // ==========================================
        // 4. SUBMISSION CENTER 
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> SubmissionCenter()
        {
            var userId = GetUserId();
            var settings = await _context.GlobalSettings.FirstOrDefaultAsync();
            ViewBag.Settings = settings;

            var project = await _context.Projects
                .Include(p => p.Submissions)
                .FirstOrDefaultAsync(p => p.StudentId == userId);

            if (project == null) return RedirectToAction("Index");

            // 1. Fetch Grade (for numeric marks)
            var grade = await _context.ProjectGrades.FirstOrDefaultAsync(g => g.ProjectId == project.Id);
            ViewBag.Grade = grade;

            // 2.Fetch Individual Panel Feedback
            var evaluations = await _context.DefenseEvaluations
                .Where(e => e.ProjectId == project.Id)
                .ToListAsync();

            ViewBag.Evaluations = evaluations;

            return View(project);
        }

        // ==========================================
        // 5. DOWNLOAD TEMPLATES
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> DownloadTemplates()
        {
            var templates = await _context.DocumentTemplates
                .OrderByDescending(t => t.UploadedAt)
                .ToListAsync();

            return View(templates);
        }
    }
}