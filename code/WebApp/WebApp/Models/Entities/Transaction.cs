using Azure;
using Azure.Data.Tables;

namespace WebApp.Models.Entities
{
    public class Transaction : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string coin { get; set; }
        public double amount { get; set; }
        public double pricePaidAll { get; set; }
        public DateTimeOffset TransactionDate { get; set; }
        public string TransactionType { get; set; } // "Buy" or "Sell"
        public string walletName { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
