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
                    // Berechne den durchschnittlichen Kaufpreis vor dem Verkauf
                    double averageBuyPrice = totalCost / totalAmount;

                    // Reduziere die Kosten um den Verkaufspreis
                    totalCost -= transaction.amount * averageBuyPrice;
                    totalAmount -= transaction.amount;
                }
            }

            return totalCost / totalAmount;
        }

        public async static Task CalculatePortfolioValueForTheLastXDays(DataFetcherService dataFetcherService,List<WalletEntry> walletEntries, List<KeyValuePair<DateTime, double>> priceList, List<KeyValuePair<string, double>> dataList, List<Transaction> allTransactionsOfUser, int days)
        {
            walletEntries.Clear();
            // Erstelle Empty WalletEntries nur mit dem Namen
            var newWalletEntries = allTransactionsOfUser
                .Select(transaction => transaction.coin)
                .Distinct()
                .Select(name => new WalletEntry { Name = name })
                .ToList();
            walletEntries.AddRange(newWalletEntries);

            // Fetch all current prices
            List<CoinPrice> coinPrices = await dataFetcherService.FetchAllCurrentPrices();

            // Use ConcurrentBag to safely collect results from parallel processing
            var results = new ConcurrentBag<Task>();

            // Perform calculations for each wallet entry in parallel
            Parallel.ForEach(walletEntries, walletEntry =>
            {
                var task = Task.Run(() =>
                {
                    var AlltransactionsOfCurrency = allTransactionsOfUser
                        .Where(transaction => transaction.coin == walletEntry.Name)
                        .OrderBy(t => t.TransactionDate)
                        .ToList();

                    walletEntry.Amount = AlltransactionsOfCurrency.Sum(transaction => transaction.TransactionType == "Buy" ? transaction.amount : -transaction.amount);

                    // Setze nur den durchschnittlichen Kaufpreis, wenn der Benutzer die Cryptocurrency besitzt
                    if (walletEntry.Amount > 0)
                    {
                        // Setze den durchschnittlichen Kaufpreis
                        walletEntry.AveragePrice = CalculateAveragePrice(AlltransactionsOfCurrency);
                    }

                    // Fetch current price
                    var coinPrice = coinPrices.FirstOrDefault(cp => cp.PartitionKey == walletEntry.Name);
                    if (coinPrice != null)
                    {
                        walletEntry.CurrentPrice = coinPrice.price;
                    }

                    // Return a placeholder result
                    return 0;
                });
                results.Add(task);
            });

            // Wait for all tasks to complete
            await Task.WhenAll(results);


            // PriceChart: Berechne den Wert des Portfolios für die letzten 180 Tage
            priceList.Clear();
            foreach (var walletEntry in walletEntries)
            {
                List<KeyValuePair<DateTime, double>> historicalPricesOfCryptoCurrency = await dataFetcherService.FetchPriceOfLastXDays(walletEntry.Name, days);

                historicalPricesOfCryptoCurrency = historicalPricesOfCryptoCurrency.OrderBy(h => h.Key).ToList();

                historicalPricesOfCryptoCurrency = CalculateEntryValueForTheLastXDays(historicalPricesOfCryptoCurrency, walletEntry.Name, allTransactionsOfUser, 180);

                // now add values to priceList
                if (priceList.Count == 0)
                {
                    priceList = historicalPricesOfCryptoCurrency;
                }
                else
                {
                    for (int j = 0; j < priceList.Count; j++)
                    {
                        priceList[j] = new KeyValuePair<DateTime, double>(priceList[j].Key, priceList[j].Value + historicalPricesOfCryptoCurrency[j].Value);
                    }
                }
            }

            
            // DoughnutChat: Füge die Werte der Allocation Liste hinzu
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


            // Hier wird für die CryptoCurrency jeder Wert des Zeitstahls berechnet. Und löscht AlltransactionsOfCurrencyOfLast180Days um performanter zu sein.
            double runningAmount = 0;
            for (int i = 0; i < historicalPricesOfCryptoCurrency.Count; i++)
            {
                var date = historicalPricesOfCryptoCurrency[i].Key;

                // Aktualisiere runningAmount basierend auf Transaktionen bis zum aktuellen historischen Preisdatum
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
