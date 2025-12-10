using FYP_App.Data;
using FYP_App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FYP_App.Controllers
{
    [Authorize(Roles = "Panel, Faculty, Supervisor, HOD, Coordinator")]
    public class PanelController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public PanelController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
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

            // 1. Get Panels 
            var myPanelIds = await _context.PanelMembers
                .Where(pm => pm.UserId == userId)
                .Select(pm => pm.PanelId)
                .ToListAsync();

            // 2. Get Defenses scheduled for those panels
            var defenses = await _context.DefenseSchedules
                .Include(d => d.Project).ThenInclude(p => p.Student)
                .Include(d => d.Panel)
                .Where(d => myPanelIds.Contains(d.PanelId))
                .OrderBy(d => d.Date)
                .ToListAsync();

            // 3. Get my past evaluations to check what I've already graded
            var myEvals = await _context.DefenseEvaluations
                .Where(e => e.EvaluatorId == userId)
                .ToListAsync();

            ViewBag.MyEvaluations = myEvals; 

            return View(defenses);
        }

        // ==========================================
        // 2. EVALUATE PROJECT (GET)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Evaluate(int projectId, string defenseType)
        {
            var project = await _context.Projects.Include(p => p.Student).FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return NotFound();

            ViewBag.DefenseType = defenseType;
            return View(project);
        }

        // ==========================================
        // 3. SUBMIT EVALUATION (POST)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> SubmitEvaluation(int projectId, string defenseType, double marks, string feedback)
        {
            var userId = GetUserId();

            // 1. Force Marks to 0 if it is Proposal Defense (Feedback only)
            if (defenseType == "Proposal Defense")
            {
                marks = 0;
            }

            // 2. Save Individual Evaluation
            var eval = new DefenseEvaluation
            {
                ProjectId = projectId,
                EvaluatorId = userId,
                DefenseType = defenseType,
                Marks = marks,
                Feedback = feedback
            };
            _context.DefenseEvaluations.Add(eval);
            await _context.SaveChangesAsync();

            // 3. Calculate Avg & Update Main Grade)
            await UpdateAggregatedGrade(projectId, defenseType);

            TempData["Success"] = "Evaluation submitted successfully.";
            return RedirectToAction("Index");
        }

        
        private async Task UpdateAggregatedGrade(int projectId, string defenseType)
        {
            // Get all evaluations for this specific defense type
            var allEvals = await _context.DefenseEvaluations
                .Where(e => e.ProjectId == projectId && e.DefenseType == defenseType)
                .ToListAsync();

            if (!allEvals.Any()) return;

            // Get or Create ProjectGrade record
            var gradeRecord = await _context.ProjectGrades.FirstOrDefaultAsync(g => g.ProjectId == projectId);
            if (gradeRecord == null)
            {
                gradeRecord = new ProjectGrade { ProjectId = projectId };
                _context.ProjectGrades.Add(gradeRecord);
            }

            // --- LOGIC FOR DIFFERENT TYPES ---

            if (defenseType == "Proposal Defense")
            {
                
            }
            else if (defenseType == "Initial Defense" || defenseType == "Midterm Defense")
            {
                // AVERAGE LOGIC: Sum of all member marks / Count
                double averageMarks = allEvals.Average(e => e.Marks);

                if (defenseType == "Initial Defense") gradeRecord.InitialDefenseMarks = averageMarks;
                else if (defenseType == "Midterm Defense") gradeRecord.MidtermDefenseMarks = averageMarks;
            }
            else if (defenseType == "Final Defense")
            {
                // FINAL LOGIC: Distinct Internal vs External
                var userId = GetUserId();
                var panelMember = await _context.PanelMembers
                    .Include(pm => pm.Panel).ThenInclude(p => p.DefenseSchedules)
                    .Where(pm => pm.UserId == userId && pm.Panel.DefenseSchedules.Any(ds => ds.ProjectId == projectId))
                    .FirstOrDefaultAsync();

                if (panelMember != null)
                {
                    // Update marks based on WHO is submitting
                    if (panelMember.Role == "Internal") gradeRecord.FinalInternalMarks = allEvals.Last().Marks;
                    else if (panelMember.Role == "External") gradeRecord.FinalExternalMarks = allEvals.Last().Marks;
                }
            }

            // Save Aggregate Feedback
            gradeRecord.DefenseFeedback = string.Join(" | ", allEvals.Select(e => e.Feedback));

            await _context.SaveChangesAsync();
        }
    }
}