using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace FYP_App.Models
{
    public class PanelMember
    {
        [Key] public int Id { get; set; }
        public int PanelId { get; set; }
        [ForeignKey("PanelId")] public Panel Panel { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")] public ApplicationUser User { get; set; }
        public string Role { get; set; } = "Member";
    }
}