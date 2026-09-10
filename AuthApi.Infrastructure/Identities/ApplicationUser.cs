using AuthApi.Domain;
using Microsoft.AspNetCore.Identity;

namespace AuthApi.Infrastructure.Identities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateOnly DateOfBirth { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        public ApplicationUser() { }

        public ApplicationUser(string firstName, string lastName, string email, string userName, DateOnly dateOfBirth)
        {
            Email = email;
            UserName = userName;
            FirstName = firstName;
            LastName = lastName;
            DateOfBirth = dateOfBirth;
        }
    }
}
