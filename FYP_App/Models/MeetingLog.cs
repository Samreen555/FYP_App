using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FYP_App.Models
{
    public class MeetingLog
    {
        [Key]
        public int Id { get; set; }

        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [Required]
        [Display(Name = "Meeting No.")]
        public int MeetingNumber { get; set; } 

        [Required]
        [DataType(DataType.Date)]
        public DateTime MeetingDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Student Activities / Minutes")]
        public string StudentActivities { get; set; } 

        [Display(Name = "Supervisor Suggestions")]
        public string? SupervisorComments { get; set; } 

        [Required]
        [Display(Name = "Next Meeting Plan")]
        public string NextMeetingPlan { get; set; } 

        // Status: "Pending", "Verified", "Rejected"
        public string Status { get; set; } = "Pending";

        // This tracks which phase the log belongs to (Initial/Midterm/Final)
        public string Phase { get; set; } = "Initial";
    }
}