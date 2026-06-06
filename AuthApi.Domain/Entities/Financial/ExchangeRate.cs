namespace AuthApi.Domain.Entities.Financial;

public class ExchangeRate : BaseEntity
{
    public ExchangeRate() : base(Guid.CreateVersion7()) { }

    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime Date { get; set; }
    public string Source { get; set; } = "external_api";
}
