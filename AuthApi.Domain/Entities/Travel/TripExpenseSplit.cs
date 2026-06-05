namespace AuthApi.Domain.Entities.Travel;

public class TripExpenseSplit : BaseEntity
{
    public TripExpenseSplit() : base(Guid.CreateVersion7()) { }

    public Guid TripExpenseId { get; set; }
    public TripExpense? TripExpense { get; set; } = null!;

    public Guid TripMemberId { get; set; }
    public TripMember? TripMember { get; set; } = null!;

    public decimal ShareAmount { get; set; }
    public string? Note { get; set; }
}
