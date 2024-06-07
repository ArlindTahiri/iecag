using Azure.Data.Tables;
using Azure;

namespace WebApp.Models.Entities
{
    public class User : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
