using AuthApi.Domain.Enums;
using AuthApi.Infrastructure.Identities;
using Microsoft.AspNetCore.Identity;

namespace AuthApi.Infrastructure.Identities.Seeds
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(RoleManager<IdentityRole<Guid>> roleManager)
        {
            foreach (var role in Enum.GetNames(typeof(UserRole)))
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>
                    {
                        Name = role,
                        NormalizedName = role.ToUpper()
                    });
                }
            }
        }

        public static async Task SeedAdminAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            var email = "nguyenthanhtuankrp1@gmail.com";

            if (await userManager.FindByEmailAsync(email) == null)
            {
                var admin = new ApplicationUser(
                    "Nguyễn Thành Tuấn",
                    20,
                    email,
                    "admin");

                admin.EmailConfirmed = true;

                await userManager.CreateAsync(admin, "Admin@123");
                await userManager.AddToRoleAsync(admin, UserRole.Admin.ToString());
            }
        }
    }
}
