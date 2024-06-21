using Azure;
using Azure.Data.Tables;
using Microsoft.ApplicationInsights;
using WebApp.HelperClasses;
using WebApp.Models.Entities;

namespace WebApp.Services
{
    public class NotificationService
    {
        private readonly TableClient _tableClient;
        private readonly ILogger<NotificationService> _logger;
        private readonly TelemetryClient _telemetryClient;

        public NotificationService(string connectionString, ILogger<NotificationService> logger, TelemetryClient telemetryClient)
        {
            var serviceClient = new TableServiceClient(connectionString);
            _tableClient = serviceClient.GetTableClient("notifications");
            _tableClient.CreateIfNotExists();
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

        public async Task CreateNotificationAsync(string userEmail, string coin, double price, string trend)
        {
            _logger.LogInformation("Creating notification for {userEmail}", userEmail);
            _telemetryClient.TrackEvent("CreateNotification", new Dictionary<string, string> { { "UserEmail", userEmail }, { "Coin", coin }, { "Price", price.ToString() }, { "Trend", trend } });

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
                _telemetryClient.TrackEvent("NotificationCreated", new Dictionary<string, string> { { "UserEmail", userEmail }, { "Coin", coin }, { "Price", price.ToString() }, { "Trend", trend } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for Email {userEmail}", userEmail);
                _telemetryClient.TrackException(ex);
                throw;
            }
        }
    }
}
