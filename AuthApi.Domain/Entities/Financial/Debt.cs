using AuthApi.Domain.Entities.Common;
using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.Financial;

public class Debt : BaseEntity, ISoftDeletable
{
    public Debt() : base(Guid.CreateVersion7()) { }

    public DateTime? DeletedAt { get; set; }

    public Guid UserId { get; set; }
    public Users? User { get; set; } = null!;

    public Guid WalletId { get; set; }
    public Wallet? Wallet { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public decimal InitialAmount { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal InterestRate { get; set; }
    public DateTime DueDate { get; set; }
    public DebtType Type { get; set; } = DebtType.CreditCard;
}
