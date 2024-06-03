namespace WebApp.Models.Entities
{
    public class TransactionEntry
    {
        public string UserId { get; set; }
        public string CryptocurrencyName { get; set; }
        public decimal Amount { get; set; }
        public decimal PricePaidAll { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; } // "Buy" or "Sell"
    }
}
