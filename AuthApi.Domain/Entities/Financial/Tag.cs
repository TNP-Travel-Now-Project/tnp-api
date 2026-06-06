using AuthApi.Domain.Entities.Common;

namespace AuthApi.Domain.Entities.Financial;

public class Tag : BaseEntity, ISoftDeletable
{
    public Tag() : base(Guid.CreateVersion7()) { }

    public DateTime? DeletedAt { get; set; }

    public Guid UserId { get; set; }
    public Users? User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public ICollection<TransactionTag> TransactionTags { get; set; } = new List<TransactionTag>();
}
