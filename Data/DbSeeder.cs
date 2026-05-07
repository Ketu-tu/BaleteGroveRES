using Microsoft.AspNetCore.Identity;
using BaleteGroveRES.Models;

namespace BaleteGroveRES.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider service)
        {
            var context = service.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();
            
            var roleManager = service.GetRequiredService<RoleManager<IdentityRole>>();
            string[] roles = { "SuperAdmin", "Admin", "Agent" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            
            var userManager = service.GetRequiredService<UserManager<ApplicationUser>>();
            var adminEmail = "superadmin@baletegrove.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    LockoutEnabled = false, 
                    DateCreated = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(newAdmin, "Admin@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "SuperAdmin");
                }
            }
        }
    }
}