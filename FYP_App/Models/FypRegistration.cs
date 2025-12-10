using System.ComponentModel.DataAnnotations;

namespace FYP_App.Models
{
    public class FypRegistration
    {
        [Key]
        public int Id { get; set; }

        // Student 1 
        [Required(ErrorMessage = "Student 1 Name is required")]
        [Display(Name = "Student 1 Name")]
        public string Student1Name { get; set; }

        [Required(ErrorMessage = "Student 1 Email is required")]
        [EmailAddress]
        public string Student1Email { get; set; }

        [Required(ErrorMessage = "Student 1 Enrollment is required")]
        [Display(Name = "Student 1 Enrollment")]
        public string Student1RegNo { get; set; }

        // --- Student 2 ---
        [Required(ErrorMessage = "Student 2 Name is required")]
        [Display(Name = "Student 2 Name")]
        public string Student2Name { get; set; }

        [Required(ErrorMessage = "Student 2 Email is required")]
        [EmailAddress]
        public string Student2Email { get; set; }

        [Required(ErrorMessage = "Student 2 Enrollment is required")]
        [Display(Name = "Student 2 Enrollment")]
        public string Student2RegNo { get; set; }

        // --- Project Details ---
        [Required(ErrorMessage = "Proposed Title is required")]
        public string ProposedTitle { get; set; }

        [Required]
        public string ProposedDomain { get; set; }

        [Required(ErrorMessage = "Preferred Supervisor is required")]
        [Display(Name = "Preferred Supervisor")]
        public string PreferredSupervisor { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.Now;
        public bool IsProcessed { get; set; } = false;
    }
}