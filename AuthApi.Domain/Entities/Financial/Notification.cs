using AuthApi.Domain.Entities.Common;
using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.Financial;

public class Notification : BaseEntity, ISoftDeletable
{
    public Notification() : base(Guid.CreateVersion7()) { }

    public DateTime? DeletedAt { get; set; }

    public Guid UserId { get; set; }
    public Users? User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.System;
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public bool IsRead { get; set; }
}
