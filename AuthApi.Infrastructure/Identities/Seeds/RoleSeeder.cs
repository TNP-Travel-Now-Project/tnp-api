using AuthApi.Domain.Enums;
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
                    "Tuấn",
                    "Nguyễn",
                    email,
                    "admin",
                    new DateOnly(2005, 8, 20));

                admin.EmailConfirmed = true;

                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (!result.Succeeded)
                {
                    throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                await userManager.AddToRoleAsync(admin, UserRole.Admin.ToString());
            }
        }
    }
}
