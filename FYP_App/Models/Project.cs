using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FYP_App.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        // 👇 Made these nullable (?) so old empty DB rows stop crashing the app
        public string? Description { get; set; }

        public string? Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Foreign Keys - made nullable to prevent crashes on unassigned projects
        public string? StudentId { get; set; }
        [ForeignKey("StudentId")]
        public virtual ApplicationUser? Student { get; set; }

        public string? SupervisorId { get; set; }
        [ForeignKey("SupervisorId")]
        public virtual ApplicationUser? Supervisor { get; set; }

        public string? Student1RegNo { get; set; }

        // 1. Submissions
        public virtual ICollection<Submission>? Submissions { get; set; }

        // 2. Defense Schedules
        public virtual ICollection<DefenseSchedule>? DefenseSchedules { get; set; }

        // 3. Meeting Logs 
        public virtual ICollection<MeetingLog>? MeetingLogs { get; set; }

        // Already perfectly nullable
        public string? WarningMessage { get; set; }
        public bool? SupervisorFlagged { get; set; }
    }
}