using AuthApi.Domain.Entities.Common;
using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.Financial;

public class RecurringTransaction : BaseEntity, ISoftDeletable
{
    public RecurringTransaction() : base(Guid.CreateVersion7()) { }

    public DateTime? DeletedAt { get; set; }

    public Guid UserId { get; set; }
    public Users? User { get; set; } = null!;

    public Guid WalletId { get; set; }
    public Wallet? Wallet { get; set; } = null!;

    public Guid? FromWalletId { get; set; }
    public Wallet? FromWallet { get; set; }

    public Guid? ToWalletId { get; set; }
    public Wallet? ToWallet { get; set; }

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; } = null!;

    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string? Note { get; set; }
    public Frequency Frequency { get; set; } = Frequency.Monthly;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime NextExecutionDate { get; set; }
    public bool IsActive { get; set; } = true;
}
