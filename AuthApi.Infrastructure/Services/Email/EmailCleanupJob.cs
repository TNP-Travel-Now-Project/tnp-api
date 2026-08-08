using AuthApi.Infrastructure.Identities;
using Microsoft.AspNetCore.Identity;

namespace AuthApi.Infrastructure.Services.Email
{
    public class EmailCleanupJob(UserManager<ApplicationUser> _userManager)
    {
        public async Task DeleteUnverifiedUser(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user != null && !user.EmailConfirmed)
            {
                await _userManager.DeleteAsync(user);
            }
        }
    }
}
