using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FYP_App.Models
{
    public class DefenseSchedule
    {
        [Key] public int Id { get; set; }
        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")] public Project Project { get; set; }
        public string DefenseType { get; set; } // Proposal, Midterm, Final
        public int PanelId { get; set; }
        [ForeignKey("PanelId")] public Panel Panel { get; set; }
        public DateTime Date { get; set; }
        public string Room { get; set; }
        public bool NotificationSent { get; set; } = false;
    }
}
