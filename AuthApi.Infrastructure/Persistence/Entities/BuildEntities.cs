using AuthApi.Domain.Entities.Chat;
using AuthApi.Domain.Entities.Common;
using AuthApi.Domain.Entities.Financial;
using AuthApi.Domain.Entities.Travel;
using AuthApi.Infrastructure.Identities;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Infrastructure.Persistence.Entities;

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

    public static void CommonEntities(this ModelBuilder builder)
    {
        builder.Entity<Users>(user =>
        {
            user.ToTable("Users");
            user.Property(u => u.Name).HasMaxLength(100).IsRequired();
            user.HasIndex(u => u.Name);
        });

        builder.Entity<Category>(category =>
        {
            category.ToTable("Categories");
            category.Property(c => c.Name).HasMaxLength(100).IsRequired();
            category.Property(c => c.Type).HasMaxLength(20).IsRequired();
            category.Property(c => c.Icon).HasMaxLength(50);
            category.Property(c => c.Color).HasMaxLength(7);

            category.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            category.HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            category.HasIndex(c => c.UserId);
            category.HasIndex(c => c.ParentCategoryId);
        });
    }

    public static void FinancialEntities(this ModelBuilder builder)
    {
        builder.Entity<Wallet>(w =>
        {
            w.ToTable("Wallets");
            w.Property(x => x.Name).HasMaxLength(100).IsRequired();
            w.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            w.Property(x => x.Balance).HasColumnType("decimal(18,2)").IsRequired();

            w.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

            w.HasIndex(x => x.UserId);
        });

        builder.Entity<Transaction>(t =>
        {
            t.ToTable("Transactions");
            t.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            t.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            t.Property(x => x.Note).HasMaxLength(1000);

            t.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            t.HasOne(x => x.Wallet).WithMany(w => w.Transactions).HasForeignKey(x => x.WalletId).OnDelete(DeleteBehavior.Restrict);
            t.HasOne(x => x.FromWallet).WithMany().HasForeignKey(x => x.FromWalletId).OnDelete(DeleteBehavior.Restrict);
            t.HasOne(x => x.ToWallet).WithMany().HasForeignKey(x => x.ToWalletId).OnDelete(DeleteBehavior.Restrict);
            t.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);

            t.HasIndex(x => new { x.UserId, x.TransactionDate });
            t.HasIndex(x => new { x.UserId, x.WalletId });
            t.HasIndex(x => new { x.UserId, x.CategoryId, x.TransactionDate });
            t.HasIndex(x => x.TransferGroupId);
        });

        builder.Entity<TransactionSplit>(s =>
        {
            s.ToTable("TransactionSplits");
            s.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            s.Property(x => x.Note).HasMaxLength(255);

            s.HasOne(x => x.Transaction).WithMany(t => t.Splits).HasForeignKey(x => x.TransactionId).OnDelete(DeleteBehavior.Cascade);
            s.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TransactionTag>(tt =>
        {
            tt.ToTable("TransactionTags");

            tt.HasOne(x => x.Transaction).WithMany(t => t.TransactionTags).HasForeignKey(x => x.TransactionId).OnDelete(DeleteBehavior.Cascade);
            tt.HasOne(x => x.Tag).WithMany(t => t.TransactionTags).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);

            tt.HasIndex(x => new { x.TransactionId, x.TagId }).IsUnique();
        });

        builder.Entity<Tag>(tag =>
        {
            tag.ToTable("Tags");
            tag.Property(x => x.Name).HasMaxLength(50).IsRequired();

            tag.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

            tag.HasIndex(x => new { x.UserId, x.Name }).IsUnique();
        });

        builder.Entity<Budget>(b =>
        {
            b.ToTable("Budgets");
            b.Property(x => x.AmountLimit).HasColumnType("decimal(18,2)").IsRequired();

            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.UserId, x.CategoryId }).IsUnique();
        });

        builder.Entity<RecurringTransaction>(r =>
        {
            r.ToTable("RecurringTransactions");
            r.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            r.Property(x => x.Note).HasMaxLength(500);

            r.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            r.HasOne(x => x.Wallet).WithMany().HasForeignKey(x => x.WalletId).OnDelete(DeleteBehavior.Restrict);
            r.HasOne(x => x.FromWallet).WithMany().HasForeignKey(x => x.FromWalletId).OnDelete(DeleteBehavior.Restrict);
            r.HasOne(x => x.ToWallet).WithMany().HasForeignKey(x => x.ToWalletId).OnDelete(DeleteBehavior.Restrict);
            r.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);

            r.HasIndex(x => new { x.UserId, x.IsActive });
            r.HasIndex(x => x.NextExecutionDate);
        });

        builder.Entity<SavingGoal>(g =>
        {
            g.ToTable("SavingGoals");
            g.Property(x => x.Name).HasMaxLength(100).IsRequired();
            g.Property(x => x.TargetAmount).HasColumnType("decimal(18,2)").IsRequired();
            g.Property(x => x.CurrentAmount).HasColumnType("decimal(18,2)").IsRequired();

            g.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            g.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
            g.HasOne(x => x.Wallet).WithMany().HasForeignKey(x => x.WalletId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Debt>(d =>
        {
            d.ToTable("Debts");
            d.Property(x => x.Name).HasMaxLength(100).IsRequired();
            d.Property(x => x.InitialAmount).HasColumnType("decimal(18,2)").IsRequired();
            d.Property(x => x.CurrentBalance).HasColumnType("decimal(18,2)").IsRequired();
            d.Property(x => x.InterestRate).HasColumnType("decimal(5,2)").IsRequired();

            d.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            d.HasOne(x => x.Wallet).WithMany().HasForeignKey(x => x.WalletId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Notification>(n =>
        {
            n.ToTable("Notifications");
            n.Property(x => x.Title).HasMaxLength(200).IsRequired();
            n.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            n.Property(x => x.ReferenceType).HasMaxLength(50);

            n.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

            n.HasIndex(x => new { x.UserId, x.IsRead });
        });

        builder.Entity<ExchangeRate>(er =>
        {
            er.ToTable("ExchangeRates");
            er.Property(x => x.FromCurrency).HasMaxLength(3).IsRequired();
            er.Property(x => x.ToCurrency).HasMaxLength(3).IsRequired();
            er.Property(x => x.Rate).HasColumnType("decimal(18,6)").IsRequired();
            er.Property(x => x.Source).HasMaxLength(50).IsRequired();

            er.HasIndex(x => new { x.FromCurrency, x.ToCurrency, x.Date }).IsUnique();
        });
    }

    public static void TravelEntities(this ModelBuilder builder)
    {
        builder.Entity<Trip>(t =>
        {
            t.ToTable("Trips");
            t.Property(x => x.Name).HasMaxLength(200).IsRequired();
            t.Property(x => x.Description).HasMaxLength(2000);
            t.Property(x => x.Destination).HasMaxLength(255).IsRequired();
            t.Property(x => x.TotalSpent).HasColumnType("decimal(18,2)").IsRequired();

            t.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);

            t.HasIndex(x => x.Status);
            t.HasIndex(x => x.CreatedById);
        });

        builder.Entity<TripMember>(m =>
        {
            m.ToTable("TripMembers");
            m.Property(x => x.Color).HasMaxLength(7);
            m.Property(x => x.Balance).HasColumnType("decimal(18,2)").IsRequired();

            m.HasOne(x => x.Trip).WithMany(t => t.Members).HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
            m.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

            m.HasIndex(x => new { x.TripId, x.UserId }).IsUnique();
        });

        builder.Entity<TripActivity>(a =>
        {
            a.ToTable("TripActivities");
            a.Property(x => x.Title).HasMaxLength(200).IsRequired();
            a.Property(x => x.Description).HasMaxLength(2000);
            a.Property(x => x.Location).HasMaxLength(500);
            a.Property(x => x.CostEstimate).HasColumnType("decimal(18,2)");

            a.HasOne(x => x.Trip).WithMany(t => t.Activities).HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);

            a.HasIndex(x => new { x.TripId, x.ActivityDate });
        });

        builder.Entity<TripExpense>(e =>
        {
            e.ToTable("TripExpenses");
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.ExchangeRateToTripCurrency).HasColumnType("decimal(18,6)");
            e.Property(x => x.AmountInTripCurrency).HasColumnType("decimal(18,2)");
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.Location).HasMaxLength(255);

            e.HasOne(x => x.Trip).WithMany(t => t.Expenses).HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.PaidBy).WithMany(m => m.PaidExpenses).HasForeignKey(x => x.PaidById).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => new { x.TripId, x.ExpenseDate });
        });

        builder.Entity<TripExpenseSplit>(s =>
        {
            s.ToTable("TripExpenseSplits");
            s.Property(x => x.ShareAmount).HasColumnType("decimal(18,2)").IsRequired();
            s.Property(x => x.Note).HasMaxLength(255);

            s.HasOne(x => x.TripExpense).WithMany(e => e.Splits).HasForeignKey(x => x.TripExpenseId).OnDelete(DeleteBehavior.Cascade);
            s.HasOne(x => x.TripMember).WithMany(m => m.Splits).HasForeignKey(x => x.TripMemberId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TripSettlement>(s =>
        {
            s.ToTable("TripSettlements");
            s.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            s.Property(x => x.Note).HasMaxLength(500);

            s.HasOne(x => x.Trip).WithMany(t => t.Settlements).HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
            s.HasOne(x => x.FromMember).WithMany().HasForeignKey(x => x.FromMemberId).OnDelete(DeleteBehavior.Restrict);
            s.HasOne(x => x.ToMember).WithMany().HasForeignKey(x => x.ToMemberId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TripDebt>(d =>
        {
            d.ToTable("TripDebts");
            d.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();

            d.HasOne(x => x.Trip).WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
            d.HasOne(x => x.FromMember).WithMany(m => m.DebtsFrom).HasForeignKey(x => x.FromMemberId).OnDelete(DeleteBehavior.Restrict);
            d.HasOne(x => x.ToMember).WithMany(m => m.DebtsTo).HasForeignKey(x => x.ToMemberId).OnDelete(DeleteBehavior.Restrict);

            d.HasIndex(x => new { x.TripId, x.FromMemberId, x.ToMemberId }).IsUnique();
        });

        builder.Entity<TripInvitation>(i =>
        {
            i.ToTable("TripInvitations");
            i.Property(x => x.InvitedEmail).HasMaxLength(255).IsRequired();
            i.Property(x => x.Token).HasMaxLength(100).IsRequired();

            i.HasOne(x => x.Trip).WithMany(t => t.Invitations).HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
            i.HasOne(x => x.InvitedBy).WithMany().HasForeignKey(x => x.InvitedById).OnDelete(DeleteBehavior.Restrict);

            i.HasIndex(x => x.Token).IsUnique();
            i.HasIndex(x => new { x.TripId, x.InvitedEmail });
        });
    }

    public static void ChatEntities(this ModelBuilder builder)
    {
        builder.Entity<TripChatRoom>(r =>
        {
            r.ToTable("TripChatRooms");

            r.HasOne(x => x.Trip).WithOne().HasForeignKey<TripChatRoom>(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
            r.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);

            r.HasIndex(x => x.TripId).IsUnique();
        });

        builder.Entity<TripMessage>(m =>
        {
            m.ToTable("TripMessages");
            m.Property(x => x.Content).HasColumnType("nvarchar(max)");
            m.Property(x => x.Metadata).HasColumnType("nvarchar(max)");

            m.HasOne(x => x.TripChatRoom).WithMany(r => r.Messages).HasForeignKey(x => x.TripChatRoomId).OnDelete(DeleteBehavior.Cascade);
            m.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.SetNull);
            m.HasOne(x => x.ReplyTo).WithMany().HasForeignKey(x => x.ReplyToId).OnDelete(DeleteBehavior.Restrict);

            m.HasIndex(x => new { x.TripChatRoomId, x.CreatedAt });
        });
    }
}
