using AuthApi.Infrastructure.Identities;
using AuthApi.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<RefreshToken> RefreshToken { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            BuildEntities.ApplicationUserEntities(builder);
        }
    }
}
