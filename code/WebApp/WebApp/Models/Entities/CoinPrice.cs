using Azure.Data.Tables;
using Azure;

namespace WebApp.Models.Entities
{
    public class CoinPrice : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public double price { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public DateTimeOffset PriceDate { get; set; }
    }
}
