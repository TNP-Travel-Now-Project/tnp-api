using AuthApi.Domain.Entities.Common;
using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.Chat;

public class TripMessage : BaseEntity, ISoftDeletable
{
    public TripMessage() : base(Guid.CreateVersion7()) { }

    public DateTime? DeletedAt { get; set; }

    public Guid TripChatRoomId { get; set; }
    public TripChatRoom? TripChatRoom { get; set; } = null!;

    public Guid? SenderId { get; set; }
    public Users? Sender { get; set; }

    public MessageType MessageType { get; set; } = MessageType.Text;
    public string? Content { get; set; }
    public string? Metadata { get; set; }
    public bool IsEdited { get; set; }

    public Guid? ReplyToId { get; set; }
    public TripMessage? ReplyTo { get; set; }
}
