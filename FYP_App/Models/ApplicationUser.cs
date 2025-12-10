using Microsoft.AspNetCore.Identity;

namespace FYP_App.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }

        public virtual UserProfile UserProfile { get; set; }
    }
}
