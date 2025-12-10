using FYP_App.Data;
using FYP_App.Models; // Ensure this is present to use LoginViewModel
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FYP_App.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        // ==========================
        // 1. LOGIN (GET) 
        // ===========================
        [HttpGet]
        public IActionResult Login(string role, string returnUrl = null)
        {
            // If the user is already logged in, redirect them to their dashboard
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToRoleDashboard();
            }

            
            if (string.IsNullOrEmpty(role)) role = "Student";

            ViewData["ReturnUrl"] = returnUrl;

            var model = new LoginViewModel
            {
                Role = role
            };

            return View(model);
        }

        // ==========================================
        // 2. LOGIN (POST) - Handles Authentication
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // A. Find the user by Email first
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    // B. SECURITY CHECK: Ensure the user actually has the role they claim.
                    // This prevents a "Coordinator" from logging in via the "Student" form.
                    bool hasRole = await _userManager.IsInRoleAsync(user, model.Role);

                    if (!hasRole)
                    {
                        ModelState.AddModelError(string.Empty, $"Access Denied: You are not authorized to login as a {model.Role}.");
                        return View(model);
                    }

                    // C. Role is verified, now we check the password
                    var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        // D. Redirect based on Role
                        return RedirectToRoleDashboard(model.Role);
                    }
                }

               
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }

        // =======================
        // 3. LOGOUT
        // ========================
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        // =============================
        // REDIRECT LOGIC
        // =============================
        private IActionResult RedirectToRoleDashboard(string role)
        {
            if (role == "Coordinator") return RedirectToAction("Index", "Coordinator");
            if (role == "Supervisor") return RedirectToAction("Index", "Supervisor");
            if (role == "Student") return RedirectToAction("Index", "Student");
            if (role == "HOD") return RedirectToAction("Index", "HOD");
            if (role == "Panel") return RedirectToAction("Index", "Panel");

            return RedirectToAction("Index", "Home");
        }

      
        private IActionResult RedirectToRoleDashboard()
        {
            if (User.IsInRole("Student")) return RedirectToAction("Index", "Student");
            if (User.IsInRole("Supervisor")) return RedirectToAction("Index", "Supervisor");
            if (User.IsInRole("Coordinator")) return RedirectToAction("Index", "Coordinator");
            if (User.IsInRole("HOD")) return RedirectToAction("Index", "HOD");
            if (User.IsInRole("Panel")) return RedirectToAction("Index", "Panel");

            return RedirectToAction("Index", "Home");
        }

        // =================================
        // 4. PUBLIC REGISTRATION 
        // ================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterGroup()
        {
            var settings = await _context.GlobalSettings.FirstOrDefaultAsync();
            bool isActive = true;

            if (settings != null)
            {
                if (!settings.RegistrationOpen || (settings.RegistrationDeadline < DateTime.Now))
                {
                    isActive = false;
                }
            }

            ViewBag.IsActive = isActive;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterGroup(FypRegistration model)
        {
            // Re-check status
            var settings = await _context.GlobalSettings.FirstOrDefaultAsync();
            if (settings != null && (!settings.RegistrationOpen || settings.RegistrationDeadline < DateTime.Now))
            {
                return Content("Error: Registration is currently closed.");
            }

            if (ModelState.IsValid)
            {
                // Check duplicates
                if (_context.FypRegistrations.Any(r => r.Student1Email == model.Student1Email || r.Student2Email == model.Student2Email))
                {
                    ModelState.AddModelError("", "One of these email addresses is already registered.");
                    ViewBag.IsActive = true;
                    return View(model);
                }

                _context.FypRegistrations.Add(model);
                await _context.SaveChangesAsync();
                return View("RegistrationSuccess");
            }

            ViewBag.IsActive = true;
            return View(model);
        }
    }
}