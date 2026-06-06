using AuthApi.Domain.Entities.Common;

namespace AuthApi.Domain.Entities.Travel;

public class TripExpense : BaseEntity, ISoftDeletable
{
    public TripExpense() : base(Guid.CreateVersion7()) { }

    public DateTime? DeletedAt { get; set; }

    public Guid TripId { get; set; }
    public Trip? Trip { get; set; } = null!;

    public Guid PaidById { get; set; }
    public TripMember? PaidBy { get; set; } = null!;

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public decimal? ExchangeRateToTripCurrency { get; set; }
    public decimal? AmountInTripCurrency { get; set; }
    public string? Note { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Location { get; set; }
    public bool IsSettled { get; set; }

    public ICollection<TripExpenseSplit> Splits { get; set; } = new List<TripExpenseSplit>();
}
