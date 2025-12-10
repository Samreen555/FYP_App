using System;

namespace FYP_App.Models
{
    public class SystemAlertViewModel
    {
        public string Type { get; set; } // "Student" or "Supervisor"
        public string ProjectTitle { get; set; }
        public string PersonName { get; set; } // Name of Student or Supervisor
        public string UserId { get; set; }
        public int ProjectId { get; set; } 
        public string Issue { get; set; } // "Missed 2 weeks", "Inactive", etc.
        public int DaysInactive { get; set; }
    }
}