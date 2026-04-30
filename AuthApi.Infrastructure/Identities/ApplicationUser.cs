using Microsoft.AspNetCore.Identity;

namespace AuthApi.Infrastructure.Identities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FullName { get; set; } = null!;
        public int Age { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ApplicationUser() { }

        public ApplicationUser(string fullName, int age, string email, string userName)
        {
            FullName = fullName;
            Age = age;
            Email = email;
            UserName = userName;
        }
    }
}
