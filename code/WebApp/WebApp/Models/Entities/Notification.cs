using Azure;
using Azure.Data.Tables;

namespace WebApp.Models.Entities
{
    public class Notification : ITableEntity
    {
        public string PartitionKey { get; set; } // "Email"
        public string RowKey { get; set; } // "Guid"
        public string coin { get; set; } // "selected Cryptocurrency"
        public double price { get; set; }
        public string trend { get; set; } // "above" or "below"
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
