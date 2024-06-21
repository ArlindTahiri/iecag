using Azure;
using Azure.Data.Tables;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using WebApp.Models.Entities;

namespace WebApp.Services
{
    public class TransactionService
    {
        private readonly TableClient _tableClient;
        private readonly ILogger<TransactionService> _logger;
        private readonly TelemetryClient _telemetryClient;

        public TransactionService(string connectionString, ILogger<TransactionService> logger, TelemetryClient telemetryClient)
        {
            var serviceClient = new TableServiceClient(connectionString);
            _tableClient = serviceClient.GetTableClient("transactions");
            _tableClient.CreateIfNotExists();
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

        public async Task CreateTransactionAsync(string userId, string coin, double amount, double pricePaidAll, DateTimeOffset transactionDate, string transactionType, string walletName)
        {
            var transaction = new Transaction
            {
                PartitionKey = userId,
                RowKey = Guid.NewGuid().ToString(),
                coin = coin,
                amount = amount,
                pricePaidAll = pricePaidAll,
                TransactionDate = transactionDate,
                TransactionType = transactionType,
                walletName = walletName,
                Timestamp = DateTimeOffset.UtcNow,
                ETag = ETag.All
            };

            _logger.LogInformation("Creating transaction for user {UserId} with amount {Amount} and coin {Coin}", userId, amount, coin);
            _telemetryClient.TrackEvent("CreateTransaction", new Dictionary<string, string>
            {
                { "UserId", userId },
                { "Coin", coin },
                { "Amount", amount.ToString() },
                { "PricePaidAll", pricePaidAll.ToString() },
                { "TransactionDate", transactionDate.ToString() },
                { "TransactionType", transactionType },
                { "WalletName", walletName }
            });

            try
            {
                await _tableClient.AddEntityAsync(transaction);
                _logger.LogInformation("Transaction created successfully for user {UserId} with RowKey {RowKey}", userId, transaction.RowKey);
                _telemetryClient.TrackEvent("TransactionCreated", new Dictionary<string, string>
                {
                    { "UserId", userId },
                    { "RowKey", transaction.RowKey }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating transaction for user {UserId}", userId);
                _telemetryClient.TrackException(ex);
                throw;
            }
        }

        public async Task<List<Transaction>> FetchAllTransactionsOfPerson(string userId)
        {
            _logger.LogInformation("Fetching all transactions for user {UserId}", userId);
            _telemetryClient.TrackEvent("FetchAllTransactions", new Dictionary<string, string> { { "UserId", userId } });

            List<Transaction> transactions = new List<Transaction>();
            await foreach (var entity in _tableClient.QueryAsync<Transaction>($"PartitionKey eq '{userId}'"))
            {
                transactions.Add(entity);
            }

            _logger.LogInformation("Fetched {Count} transactions for user {UserId}", transactions.Count, userId);
            _telemetryClient.TrackEvent("FetchedAllTransactions", new Dictionary<string, string>
            {
                { "UserId", userId },
                { "Count", transactions.Count.ToString() }
            });

            return transactions;
        }
    }
}
