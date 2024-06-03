using WebApp.Models.Entities;

namespace WebApp
{
    public static class FinanceCalculations
    {
        public static decimal CalculateAveragePrice(List<TransactionEntry> transactionEntries)
        {
            decimal totalAmount = 0;
            decimal totalCost = 0;

            foreach (var transaction in transactionEntries)
            {
                if (transaction.TransactionType == "Buy")
                {
                    totalCost += transaction.PricePaidAll;
                    totalAmount += transaction.Amount;
                }
                else if (transaction.TransactionType == "Sell")
                {
                    // Berechne den durchschnittlichen Kaufpreis vor dem Verkauf
                    decimal averageBuyPrice = totalCost / totalAmount;

                    // Reduziere die Kosten um den Verkaufspreis
                    totalCost -= transaction.Amount * averageBuyPrice;
                    totalAmount -= transaction.Amount;
                }
            }

            return totalCost / totalAmount;
        }

        public static List<KeyValuePair<DateTime, decimal>> CalculatePortfolioValueForTheLastXDays(List<KeyValuePair<DateTime, decimal>> historicalPricesOfCryptoCurrency, string CryptcurrencyName, List<TransactionEntry> allTransactionsOfUser, int days)
        {
            var AlltransactionsOfCurrencyOfLastXDays = allTransactionsOfUser
            .Where(transaction => transaction.CryptocurrencyName == CryptcurrencyName && transaction.TransactionDate <= DateTime.Today.AddDays(days))
            .OrderBy(t => t.TransactionDate)
            .ToList();


            // Hier wird für die CryptoCurrency jeder Wert des Zeitstahls berechnet. Und löscht AlltransactionsOfCurrencyOfLast180Days um performanter zu sein.
            decimal runningAmount = 0;
            for (int i = 0; i < historicalPricesOfCryptoCurrency.Count; i++)
            {
                var date = historicalPricesOfCryptoCurrency[i].Key;

                // Aktualisiere runningAmount basierend auf Transaktionen bis zum aktuellen historischen Preisdatum
                while (AlltransactionsOfCurrencyOfLastXDays.Count > 0 && AlltransactionsOfCurrencyOfLastXDays[0].TransactionDate <= date)
                {
                    var transaction = AlltransactionsOfCurrencyOfLastXDays[0];
                    runningAmount += transaction.TransactionType == "Buy" ? transaction.Amount : -transaction.Amount;
                    AlltransactionsOfCurrencyOfLastXDays.RemoveAt(0);
                }

                historicalPricesOfCryptoCurrency[i] = new KeyValuePair<DateTime, decimal>(date, historicalPricesOfCryptoCurrency[i].Value * runningAmount);
            }

            return historicalPricesOfCryptoCurrency;
        }
    }
}
