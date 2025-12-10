using System.ComponentModel.DataAnnotations;

namespace FYP_App.Models
{
    public class DocumentTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } 

        public string? Description { get; set; } // Optional description

        [Required]
        public string FilePath { get; set; } 

        
        public string? FileName { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}