using AuthApi.Domain.Entities.Common;

namespace AuthApi.Domain.Entities.Chat;

public class TripChatRoom : BaseEntity
{
    public TripChatRoom() : base(Guid.CreateVersion7()) { }

    public Guid TripId { get; set; }
    public Travel.Trip? Trip { get; set; } = null!;

    public Users? CreatedBy { get; set; } = null!;

    public ICollection<TripMessage> Messages { get; set; } = new List<TripMessage>();
}
