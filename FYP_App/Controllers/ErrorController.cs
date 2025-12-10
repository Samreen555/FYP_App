using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FYP_App.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            switch (statusCode)
            {
                case 404:
                    ViewBag.ErrorMessage = "Sorry, the resource you requested could not be found.";
                    ViewBag.ErrorTitle = "Page Not Found";
                    break;
                case 500:
                    ViewBag.ErrorMessage = "An internal server error has occurred. Please contact support.";
                    ViewBag.ErrorTitle = "Server Error";
                    break;
                default:
                    ViewBag.ErrorMessage = "An unexpected error occurred.";
                    ViewBag.ErrorTitle = "Error";
                    break;
            }
            return View("Error");
        }

        [Route("Error")]
        public IActionResult Error()
        {
            return View();
        }
    }
}