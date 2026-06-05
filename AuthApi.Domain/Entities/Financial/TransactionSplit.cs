using AuthApi.Domain.Entities.Common;

namespace AuthApi.Domain.Entities.Financial;

public class TransactionSplit : BaseEntity
{
    public TransactionSplit() : base(Guid.CreateVersion7()) { }

    public Guid TransactionId { get; set; }
    public Transaction? Transaction { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; } = null!;

    public decimal Amount { get; set; }
    public string? Note { get; set; }
}
