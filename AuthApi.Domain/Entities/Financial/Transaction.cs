using AuthApi.Domain.Entities.Common;
using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.Financial;

public class Transaction : BaseEntity, ISoftDeletable, IAggregateRoot
{
    public Transaction() : base(Guid.CreateVersion7()) { }

    public DateTime? DeletedAt { get; set; }

    public Guid UserId { get; set; }
    public Users? User { get; set; } = null!;

    public Guid WalletId { get; set; }
    public Wallet? Wallet { get; set; } = null!;

    public Guid? FromWalletId { get; set; }
    public Wallet? FromWallet { get; set; }

    public Guid? ToWalletId { get; set; }
    public Wallet? ToWallet { get; set; }

    public Guid? TransferGroupId { get; set; }

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; } = null!;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public TransactionType Type { get; set; }
    public string? Note { get; set; }
    public DateTime TransactionDate { get; set; }

    public ICollection<TransactionSplit> Splits { get; set; } = new List<TransactionSplit>();
    public ICollection<TransactionTag> TransactionTags { get; set; } = new List<TransactionTag>();
}
