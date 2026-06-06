using AuthApi.Domain.Entities.Common;
using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.Travel;

public class Trip : BaseEntity, ISoftDeletable, IAggregateRoot
{
    public Trip() : base(Guid.CreateVersion7()) { }

    public DateTime? DeletedAt { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Destination { get; set; } = string.Empty;
    public TripStatus Status { get; set; } = TripStatus.Planning;
    public decimal TotalSpent { get; set; }

    public Users? CreatedBy { get; set; } = null!;

    public ICollection<TripMember> Members { get; set; } = new List<TripMember>();
    public ICollection<TripActivity> Activities { get; set; } = new List<TripActivity>();
    public ICollection<TripExpense> Expenses { get; set; } = new List<TripExpense>();
    public ICollection<TripSettlement> Settlements { get; set; } = new List<TripSettlement>();
    public ICollection<TripInvitation> Invitations { get; set; } = new List<TripInvitation>();
}
