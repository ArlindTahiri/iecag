using Azure;
using Azure.Data.Tables;
using WebApp.HelperClasses;
using WebApp.Models.Entities;

namespace WebApp.Services
{

    public class NotificationService
    {
        private readonly TableClient _tableClient;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(string connectionString, ILogger<NotificationService> logger)
        {
            var serviceClient = new TableServiceClient(connectionString);
            _tableClient = serviceClient.GetTableClient("notifications");
            _tableClient.CreateIfNotExists();
            _logger = logger;
        }

        public async Task CreateNotificationAsync(string userEmail, string coin, double price, string trend)
        {
            _logger.LogInformation("Creating notification for {userEmail}", userEmail);


            var notification = new Notification
            {
                PartitionKey = userEmail,
                RowKey = Guid.NewGuid().ToString(),
                coin = coin,
                price = price,
                trend = trend,
                Timestamp = DateTimeOffset.UtcNow,
                ETag = ETag.All
            };

            try
            {
                await _tableClient.AddEntityAsync(notification);
                _logger.LogInformation("Notification for Email {userEmail} created successfully", userEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for Email {userEmail}", userEmail);
                throw;
            }
        }

    }

}
