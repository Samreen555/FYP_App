using Microsoft.AspNetCore.Mvc;

namespace FYP_App.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Coordinator")) return RedirectToAction("Index", "Coordinator");
                if (User.IsInRole("Student")) return RedirectToAction("Index", "Student");
                if (User.IsInRole("Supervisor")) return RedirectToAction("Index", "Supervisor");
                if (User.IsInRole("HOD")) return RedirectToAction("Index", "HOD");
                if (User.IsInRole("Panel")) return RedirectToAction("Index", "Panel");
            }

           
            return RedirectToAction("Login", "Account");
        }
    }
}