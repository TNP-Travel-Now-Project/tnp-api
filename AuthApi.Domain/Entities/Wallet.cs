using AuthApi.Domain;
using AuthApi.Domain.Entities;

namespace AuthApi.Infrastructure.Persistence.Entities
{
    public class Wallet : BaseEntity
    {
        public Guid UserId { get; set; }
        public Users User { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public decimal Balance { get; set; } = 0;           // CACHE ONLY
        public string Currency { get; set; } = "VND";
        public string Type { get; set; } = "Cash";          // Cash, Bank, Credit, ...

        // Navigation
        //public ICollection<Transaction> Transactions { get; set; } = new();
    }
}
