using AuthApi.Domain.Entities.Common;
using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.Travel;

public class TripMember : BaseEntity, ISoftDeletable
{
    public TripMember() : base(Guid.CreateVersion7()) { }

    public DateTime? DeletedAt { get; set; }

    public Guid TripId { get; set; }
    public Trip? Trip { get; set; } = null!;

    public Guid UserId { get; set; }
    public Users? User { get; set; } = null!;

    public TripMemberRole Role { get; set; } = TripMemberRole.Member;
    public InvitationStatus InvitationStatus { get; set; } = InvitationStatus.Pending;
    public DateTime? JoinedAt { get; set; }
    public decimal Balance { get; set; }
    public string? Color { get; set; }

    public ICollection<TripExpenseSplit> Splits { get; set; } = new List<TripExpenseSplit>();
    public ICollection<TripExpense> PaidExpenses { get; set; } = new List<TripExpense>();
    public ICollection<TripDebt> DebtsFrom { get; set; } = new List<TripDebt>();
    public ICollection<TripDebt> DebtsTo { get; set; } = new List<TripDebt>();
}
