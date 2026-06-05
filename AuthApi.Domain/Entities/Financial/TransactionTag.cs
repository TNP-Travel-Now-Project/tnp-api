namespace AuthApi.Domain.Entities.Financial;

public class TransactionTag : BaseEntity
{
    public TransactionTag() : base(Guid.CreateVersion7()) { }

    public Guid TransactionId { get; set; }
    public Transaction? Transaction { get; set; } = null!;

    public Guid TagId { get; set; }
    public Tag? Tag { get; set; } = null!;
}
