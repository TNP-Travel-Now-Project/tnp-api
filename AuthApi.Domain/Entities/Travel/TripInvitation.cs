using AuthApi.Domain.Entities.Common;
using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.Travel;

public class TripInvitation : BaseEntity
{
    public TripInvitation() : base(Guid.CreateVersion7()) { }

    public Guid TripId { get; set; }
    public Trip? Trip { get; set; } = null!;

    public string InvitedEmail { get; set; } = string.Empty;

    public Guid InvitedById { get; set; }
    public Users? InvitedBy { get; set; } = null!;

    public string Token { get; set; } = string.Empty;
    public InvitationStatus InvitationStatus { get; set; } = InvitationStatus.Pending;
    public DateTime ExpiresAt { get; set; }
    public DateTime? EmailSentAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
}
