using AuthApi.Domain.ObjectValues;
using AuthApi.Infrastructure.Identities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Reflection.Emit;

namespace AuthApi.Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<RefreshToken> RefreshToken { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            var ageUserConverter = new ValueConverter<AgeUser, int>(v => v.Value, v => AgeUser.Create(v));

            builder.Entity<ApplicationUser>(user =>
            {
                user.ToTable("AspNetUsers");

                user.Property(u => u.FullName)
                    .HasMaxLength(150)
                    .IsRequired();

                user.Property(p => p.Age)
                   .HasConversion(ageUserConverter)
                   .HasColumnName("Age")
                   .IsRequired();


                user.Property(u => u.CreatedAt).IsRequired();
                user.Property(u => u.UpdatedAt);
            });
        }
    }
}
