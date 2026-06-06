using AuthApi.Domain.Entities.Common;

namespace AuthApi.Domain.Entities.Financial;

public class SavingGoal : BaseEntity, ISoftDeletable
{
    public SavingGoal() : base(Guid.CreateVersion7()) { }

    public DateTime? DeletedAt { get; set; }

    public Guid UserId { get; set; }
    public Users? User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateTime? TargetDate { get; set; }

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public Guid? WalletId { get; set; }
    public Wallet? Wallet { get; set; }
}
