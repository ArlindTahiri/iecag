using Azure;
using Azure.Data.Tables;
using WebApp.HelperClasses;
using WebApp.Models.Entities;

namespace WebApp.Services
{

    public class TransactionService
    {
        private readonly TableClient _tableClient;

        public TransactionService(string connectionString)
        {
            var serviceClient = new TableServiceClient(connectionString);
            _tableClient = serviceClient.GetTableClient("transactions");
            _tableClient.CreateIfNotExists();
        }

        public async Task CreateTransactionAsync(string userId, string coin, double amount, double pricePaidAll, DateTimeOffset transactionDate, string TransactionType, string walletName)
        {
            var transaction = new Transaction
            {
                PartitionKey = userId,
                RowKey = Guid.NewGuid().ToString(),
                coin = coin,
                amount = amount,
                pricePaidAll = pricePaidAll,
                TransactionDate = transactionDate,
                TransactionType = TransactionType,
                walletName = walletName,
                Timestamp = DateTimeOffset.UtcNow,
                ETag = ETag.All
            };

            await _tableClient.AddEntityAsync(transaction);
        }


        public async Task<List<Transaction>> FetchAllTransactionsOfPerson(string userId)
        {
            List<Transaction> transactions = new List<Transaction>();
            await foreach(var entity in _tableClient.QueryAsync<Transaction>($"PartitionKey eq '{userId}'"))
            {
                transactions.Add(entity);
            }
            return transactions;
        }
    }

}
