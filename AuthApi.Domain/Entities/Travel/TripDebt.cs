namespace AuthApi.Domain.Entities.Travel;

public class TripDebt : BaseEntity
{
    public TripDebt() : base(Guid.CreateVersion7()) { }

    public Guid TripId { get; set; }
    public Trip? Trip { get; set; } = null!;

    public Guid FromMemberId { get; set; }
    public TripMember? FromMember { get; set; } = null!;

    public Guid ToMemberId { get; set; }
    public TripMember? ToMember { get; set; } = null!;

    public decimal Amount { get; set; }
    public DateTime LastCalculatedAt { get; set; }
}
