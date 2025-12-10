using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FYP_App.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        public string Role { get; set; } // Student, Supervisor, etc.
        public string Department { get; set; }
        public string RegistrationNumber { get; set; }
    }
}