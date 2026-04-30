using AuthApi.Infrastructure.Identities;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Infrastructure.Persistence.Entities
{
    public static class BuildEntities
    {
        public static void ApplicationUserEntities(this ModelBuilder builder)
        {
            builder.Entity<ApplicationUser>(user =>
            {
                user.ToTable("AspNetUsers");

                user.Property(u => u.FirstName)
                    .HasMaxLength(50)
                        .IsRequired();

                user.Property(u => u.LastName)
                    .HasMaxLength(50)
                    .IsRequired();

                user.Property(p => p.DateOfBirth)
                   .HasColumnName("DOB")
                   .HasColumnType("date")
                   .IsRequired();


                user.Property(u => u.CreatedAt).IsRequired();
                user.Property(u => u.UpdatedAt);
            });
        }
    }
}
