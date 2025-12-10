using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FYP_App.Models
{
    public class DefenseEvaluation
    {
        [Key]
        public int Id { get; set; }

        public int ProjectId { get; set; }

        public string EvaluatorId { get; set; } 

        public string DefenseType { get; set; } 

        public double Marks { get; set; } 

        public string Feedback { get; set; } 
    }
}