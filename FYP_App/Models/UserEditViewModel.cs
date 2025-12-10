using System.ComponentModel.DataAnnotations;

namespace FYP_App.Models
{
    public class UserEditViewModel
    {
        public string Id { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}