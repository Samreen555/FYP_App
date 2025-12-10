using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FYP_App.Models
{
    public class ProjectGrade
    {
        [Key]
        public int Id { get; set; }

        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        public double? InitialDefenseMarks { get; set; }
        public double? MidtermDefenseMarks { get; set; }
        public double? SupervisorMarks { get; set; }

        public double? CoordinatorMarks { get; set; }

        public double? FinalInternalMarks { get; set; }
        public double? FinalExternalMarks { get; set; }

        public string? Grade { get; set; }
        public double? TotalMarks { get; set; }

        public string? DefenseFeedback { get; set; }
    }
}