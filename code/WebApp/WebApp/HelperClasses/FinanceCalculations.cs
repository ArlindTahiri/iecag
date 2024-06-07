using WebApp.Models.Entities;

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

        public static List<KeyValuePair<DateTime, double>> CalculatePortfolioValueForTheLastXDays(List<KeyValuePair<DateTime, double>> historicalPricesOfCryptoCurrency, string CryptcurrencyName, List<Transaction> allTransactionsOfUser, int days)
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
