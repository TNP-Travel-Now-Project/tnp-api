using AuthApi.Domain.Enums;
using AuthApi.Domain.ObjectValues;
using Microsoft.AspNetCore.Identity;

namespace AuthApi.Infrastructure.Identities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.User;
        public AgeUser Age { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ApplicationUser() { }

        public ApplicationUser(string fullName, int age, UserRole role, string email, string userName)
        {
            FullName = fullName;
            Age = AgeUser.Create(age);
            Role = role;
            Email = email;
            UserName = userName;
            EmailConfirmed = true;
        }
    }
}
