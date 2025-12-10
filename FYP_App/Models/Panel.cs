using System.ComponentModel.DataAnnotations;
namespace FYP_App.Models
{
    public class Panel
    {
        [Key] public int Id { get; set; }
        [Required] public string Name { get; set; }
        public bool HODApproval { get; set; } = false;
        public List<PanelMember> Members { get; set; }
        public virtual ICollection<DefenseSchedule> DefenseSchedules { get; set; }
    }
}