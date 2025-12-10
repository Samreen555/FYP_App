using System.ComponentModel.DataAnnotations;

namespace FYP_App.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Please select a role.")]
        public string Role { get; set; } // Student, Supervisor, Coordinator, etc.

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}