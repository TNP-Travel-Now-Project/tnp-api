using AuthApi.Domain.Entities.Common;
using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.Financial;

public class Wallet : BaseEntity, ISoftDeletable, IAggregateRoot
{
    public Wallet() : base(Guid.CreateVersion7()) { }

    public DateTime? DeletedAt { get; set; }

    public Guid UserId { get; set; }
    public Users? User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "VND";
    public WalletType Type { get; set; } = WalletType.Cash;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
