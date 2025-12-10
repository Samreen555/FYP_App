using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FYP_App.Models
{
    public class Submission
    {
        [Key]
        public int Id { get; set; }

        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }

        [Required]
        public string SubmissionType { get; set; } // e.g. "Proposal", "SRS", "Final Report"

        [Required]
        public string FilePath { get; set; }

        public string? Remarks { get; set; } // Comments from Supervisor/Coordinator

        // Workflow Status: 
        // "Pending Supervisor" -> "Pending Coordinator" -> "Approved" (or "Rejected")
        public string Status { get; set; } = "Pending Supervisor";

        public DateTime SubmittedAt { get; set; } = DateTime.Now;
    }
}