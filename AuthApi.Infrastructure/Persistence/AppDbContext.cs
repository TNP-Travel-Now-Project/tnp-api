using AuthApi.Domain;
using AuthApi.Domain.Entities.Chat;
using AuthApi.Domain.Entities.Common;
using AuthApi.Domain.Entities.Financial;
using AuthApi.Domain.Entities.Travel;
using AuthApi.Infrastructure.Identities;
using AuthApi.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace AuthApi.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public DbConnection Connection => Database.GetDbConnection();

    private readonly HashSet<object> _forceHardDelete = new();
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<RefreshToken> RefreshToken { get; set; } = null!;

    public DbSet<Users> AppUsers { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;

    public DbSet<Wallet> Wallets { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<TransactionSplit> TransactionSplits { get; set; } = null!;
    public DbSet<TransactionTag> TransactionTags { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;
    public DbSet<Budget> Budgets { get; set; } = null!;
    public DbSet<RecurringTransaction> RecurringTransactions { get; set; } = null!;
    public DbSet<SavingGoal> SavingGoals { get; set; } = null!;
    public DbSet<Debt> Debts { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<ExchangeRate> ExchangeRates { get; set; } = null!;

    public DbSet<Trip> Trips { get; set; } = null!;
    public DbSet<TripMember> TripMembers { get; set; } = null!;
    public DbSet<TripActivity> TripActivities { get; set; } = null!;
    public DbSet<TripExpense> TripExpenses { get; set; } = null!;
    public DbSet<TripExpenseSplit> TripExpenseSplits { get; set; } = null!;
    public DbSet<TripSettlement> TripSettlements { get; set; } = null!;
    public DbSet<TripDebt> TripDebts { get; set; } = null!;
    public DbSet<TripInvitation> TripInvitations { get; set; } = null!;

    public DbSet<TripChatRoom> TripChatRooms { get; set; } = null!;
    public DbSet<TripMessage> TripMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplicationUserEntities();
        builder.CommonEntities();
        builder.FinancialEntities();
        builder.TravelEntities();
        builder.ChatEntities();
        builder.SoftDeleteEntities();
    }

    public void HardDelete<T>(T entity) where T : class, ISoftDeletable
    {
        _forceHardDelete.Add(entity);
        Entry(entity).State = EntityState.Deleted;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Deleted
                && entry.Entity is ISoftDeletable soft
                && !_forceHardDelete.Contains(entry.Entity))
            {
                entry.State = EntityState.Modified;
                soft.DeletedAt = DateTime.UtcNow;
            }
        }

        _forceHardDelete.Clear();
        return await base.SaveChangesAsync(cancellationToken);
    }
}
