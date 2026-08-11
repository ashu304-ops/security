using Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Persistence.Seeders;

public static class DbSeeder
{
    public static async Task SeedSuperAdminAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string superAdminEmail = "admin@computerseekho.com";

        var existingAdmin = await userManager.FindByEmailAsync(superAdminEmail);
        if (existingAdmin == null)
        {
            var adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = superAdminEmail,
                Email = superAdminEmail,
                StaffName = "System SuperAdmin",
                Department = "Administration",
                EmailConfirmed = true,
                IsActive = true,
                IsMfaEnabled = false,   // Ensures MFA is explicitly disabled on fresh seed
                MfaSecretKey = null     // Clears any leftover secret key
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@1234");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
            }
        }
    }
}