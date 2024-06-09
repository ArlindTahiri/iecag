using System.Collections.Concurrent;
using WebApp.Models.Entities;
using WebApp.Services;

namespace WebApp.HelperClasses
{
    public static class FinanceCalculations
    {
        public static double CalculateAveragePrice(List<Transaction> transactionEntries)
        {
            double totalAmount = 0;
            double totalCost = 0;

            foreach (var transaction in transactionEntries)
            {
                if (transaction.TransactionType == "Buy")
                {
                    totalCost += transaction.pricePaidAll;
                    totalAmount += transaction.amount;
                }
                else if (transaction.TransactionType == "Sell")
                {
                    // Calculate the average buy price before the sale
                    double averageBuyPrice = totalCost / totalAmount;

                    // reduce the total cost by the average price
                    totalCost -= transaction.amount * averageBuyPrice;
                    totalAmount -= transaction.amount;
                }
            }

            return totalCost / totalAmount;
        }

        public async static Task CalculatePortfolioValueForTheLastXDays(DataFetcherService dataFetcherService,List<WalletEntry> walletEntries, List<KeyValuePair<DateTime, double>> priceList, List<KeyValuePair<string, double>> dataList, List<Transaction> allTransactionsOfUser, int days)
        {
            walletEntries.Clear();

            // Fetch all unique coins of the user
            var newWalletEntries = allTransactionsOfUser
                .Select(transaction => transaction.coin)
                .Distinct()
                .Select(name => new WalletEntry { Name = name })
                .ToList();
            walletEntries.AddRange(newWalletEntries);

            // Fetch all current prices
            List<CoinPrice> coinPrices = await dataFetcherService.FetchAllCurrentPrices();


            // Perform calculations for each wallet entry in parallel
            var tasks = walletEntries.Select(async walletEntry =>
            {
                var allTransactionsOfCurrency = allTransactionsOfUser
                    .Where(transaction => transaction.coin == walletEntry.Name)
                    .OrderBy(t => t.TransactionDate)
                    .ToList();

                walletEntry.Amount = allTransactionsOfCurrency.Sum(transaction => transaction.TransactionType == "Buy" ? transaction.amount : -transaction.amount);

                if (walletEntry.Amount > 0)
                {
                    walletEntry.AveragePrice = CalculateAveragePrice(allTransactionsOfCurrency);
                }

                var coinPrice = coinPrices.FirstOrDefault(cp => cp.PartitionKey == walletEntry.Name);
                if (coinPrice != null)
                {
                    walletEntry.CurrentPrice = coinPrice.price;
                }
            });

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            var orderedEntries = walletEntries.OrderByDescending(w => w.Amount * w.CurrentPrice).ToList();
            walletEntries.Clear();
            walletEntries.AddRange(orderedEntries);

            // PriceChart: Fetch historical prices for each wallet entry and calculate the value of the wallet entry for the last X days

            // Prepare tasks to fetch historical prices for each wallet entry and calculate the value of the wallet entry for the last X days
            var priceFetchingTasks = walletEntries.Select(async walletEntry =>
            {
                List<KeyValuePair<DateTime, double>> historicalPricesOfCryptoCurrency = await dataFetcherService.FetchPriceOfLastXDays(walletEntry.Name, days);
                historicalPricesOfCryptoCurrency = CalculateEntryValueForTheLastXDays(historicalPricesOfCryptoCurrency.OrderBy(h => h.Key).ToList(), walletEntry.Name, allTransactionsOfUser, days);
                return historicalPricesOfCryptoCurrency;
            });

            // Wait for all tasks to complete and collect results
            var fetchedPrices = await Task.WhenAll(priceFetchingTasks);

            // Aggregate fetched prices into priceList
            priceList.Clear();
            if (fetchedPrices.Length > 0)
            {
                // Initialize priceList with the first set of prices
                priceList.AddRange(fetchedPrices[0]);

                for (int i = 1; i < fetchedPrices.Length; i++)
                {
                    var pricesOfCryptoCurrency = fetchedPrices[i];
                    if (pricesOfCryptoCurrency.Count != priceList.Count)
                    {
                        // Handle the case where the counts do not match
                        throw new InvalidOperationException("Mismatched price data lengths");
                    }

                    for (int j = 0; j < priceList.Count; j++)
                    {
                        priceList[j] = new KeyValuePair<DateTime, double>(priceList[j].Key, priceList[j].Value + pricesOfCryptoCurrency[j].Value);
                    }
                }
            }


            // DoughnutChart: Add the values of the Allocation list
            dataList.Clear();
            foreach (var walletEntry in walletEntries)
            {
                dataList.Add(new KeyValuePair<string, double>(walletEntry.Name, walletEntry.CurrentPrice * walletEntry.Amount));
            }
        }

        public static List<KeyValuePair<DateTime, double>> CalculateEntryValueForTheLastXDays(List<KeyValuePair<DateTime, double>> historicalPricesOfCryptoCurrency, string CryptcurrencyName, List<Transaction> allTransactionsOfUser, int days)
        {
            var AlltransactionsOfCurrencyOfLastXDays = allTransactionsOfUser
            .Where(transaction => transaction.coin == CryptcurrencyName && transaction.TransactionDate <= DateTime.Today.AddDays(days))
            .OrderBy(t => t.TransactionDate)
            .ToList();



            // Here, for the CryptoCurrency, every value of the timestamp is calculated. And deletes AlltransactionsOfCurrencyOfLastXDays to be more performant.
            double runningAmount = 0;
            for (int i = 0; i < historicalPricesOfCryptoCurrency.Count; i++)
            {
                var date = historicalPricesOfCryptoCurrency[i].Key;

                // Update runningAmount based on transactions until the current historical price date
                while (AlltransactionsOfCurrencyOfLastXDays.Count > 0 && AlltransactionsOfCurrencyOfLastXDays[0].TransactionDate <= date)
                {
                    var transaction = AlltransactionsOfCurrencyOfLastXDays[0];
                    runningAmount += transaction.TransactionType == "Buy" ? transaction.amount : -transaction.amount;
                    AlltransactionsOfCurrencyOfLastXDays.RemoveAt(0);
                }

                historicalPricesOfCryptoCurrency[i] = new KeyValuePair<DateTime, double>(date, historicalPricesOfCryptoCurrency[i].Value * runningAmount);
            }

            return historicalPricesOfCryptoCurrency;
        }
    }
}
