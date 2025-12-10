using System.ComponentModel.DataAnnotations;

namespace FYP_App.Models
{
    public class GlobalSettings
    {
        [Key]
        public int Id { get; set; }

        public bool RegistrationOpen { get; set; }
        public DateTime RegistrationDeadline { get; set; }

        // SPECIFIC DEADLINES 
        [Display(Name = "Proposal Submission")]
        public DateTime? ProposalDeadline { get; set; }

        [Display(Name = "SRS Document")]
        public DateTime? SRSDeadline { get; set; }

        [Display(Name = "SDS Document")]
        public DateTime? SDSDeadline { get; set; }

        [Display(Name = "Meeting Log Sheet")]
        public DateTime? MeetingLogDeadline { get; set; }

        [Display(Name = "Final Report")]
        public DateTime? FinalReportDeadline { get; set; }

        public string? DefaultSubmissionDeadlines { get; set; }

        public int WarningThresholdDays { get; set; } = 3;
    }
}