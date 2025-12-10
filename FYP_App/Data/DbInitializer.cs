using FYP_App.Models;
using Microsoft.AspNetCore.Identity;

namespace FYP_App.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = { "Student", "Supervisor", "Coordinator", "Panel", "HOD", "Admin" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Default Coordinator
            var coordEmail = "coordinator@fyp.com";
            if (await userManager.FindByEmailAsync(coordEmail) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = coordEmail,
                    Email = coordEmail,
                    FullName = "Chief Coordinator",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(user, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Coordinator");
                    
                }
            }
        }
    }
}