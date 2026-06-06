using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities.Travel;

public class TripActivity : BaseEntity, ISoftDeletable
{
    public TripActivity() : base(Guid.CreateVersion7()) { }

    public DateTime? DeletedAt { get; set; }

    public Guid TripId { get; set; }
    public Trip? Trip { get; set; } = null!;

    public DateTime ActivityDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public ActivityCategory Category { get; set; } = ActivityCategory.Sightseeing;
    public decimal? CostEstimate { get; set; }
    public ActivityStatus Status { get; set; } = ActivityStatus.Planned;
    public int Order { get; set; }
}
