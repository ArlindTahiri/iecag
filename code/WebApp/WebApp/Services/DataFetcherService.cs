using Azure.Data.Tables;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WebApp.Models.Entities;

namespace WebApp.Services
{
    public class DataFetcherService
    {
        private readonly HttpClient _httpClient;
        private Timer _timer;
        private readonly object _lock = new object();
        private readonly IHubContext<PricesHub> _hubContext;

        public DataFetcherService(HttpClient httpClient, IHubContext<PricesHub> hubContext)
        {
            _httpClient = httpClient;
            _hubContext = hubContext;
            _timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        private async void TimerCallback(object state)
        {
            lock (_lock)
            {
                // Code to fetch data from backend
                //FetchCurrentPricesFromAzureTable();
                /*
                FetchPriceFromBackend("https://api.coingecko.com/api/v3/coins/crypto-com-chain", "Cronos");
                FetchPriceFromBackend("https://api.coingecko.com/api/v3/coins/bitcoin", "Bitcoin");
                FetchPriceFromBackend("https://api.coingecko.com/api/v3/coins/ethereum", "Ethereum");
                */
            }
        }

        /*
        public async Task FetchCurrentPricesFromAzureTable()
        {
            // Verbindung zu Azure Table Storage herstellen
            var serviceClient = new TableServiceClient(new Uri(_options.TableEndpoint), new TableSharedKeyCredential(_options.AccountName, _options.AccountKey));
            var tableClient = serviceClient.GetTableClient("currentprices");

            // Lade alle Einträge aus der Tabelle
            await foreach (var entity in tableClient.QueryAsync<TableEntity>())
            {
                await _hubContext.Clients.All.SendAsync("ReceivePriceUpdate", entity.PartitionKey, Convert.ToDecimal(entity["price"]));
            }
        }
        */

        public async Task FetchPriceFromBackend(string url, string name)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                

                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    var obj = JsonSerializer.Deserialize<CoinGeckoResponse>(responseData);
                    decimal? price = obj?.market_data?.current_price?.eur;
                    await _hubContext.Clients.All.SendAsync("ReceivePriceUpdate", name, price);
                }
                else
                {
                    Random random = new Random();
                    if (name.Equals("Cronos"))
                    {
                        await _hubContext.Clients.All.SendAsync("ReceivePriceUpdate", name, (decimal)random.Next(90, 130)/1000);
                    }
                    if (name.Equals("Bitcoin"))
                    {
                        await _hubContext.Clients.All.SendAsync("ReceivePriceUpdate", name, random.Next(60000, 63000));
                    }
                    if (name.Equals("Ethereum"))
                    {
                        await _hubContext.Clients.All.SendAsync("ReceivePriceUpdate", name, random.Next(3400, 3600));
                    }
                    
                    Console.WriteLine($"Failed to fetch data from {url}. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while fetching data from {url}: {ex.Message}");
            }
        }


        public class CoinGeckoResponse
        {
            public MarketData market_data { get; set; }
        }

        public class MarketData
        {
            public PriceInfo current_price { get; set; }
        }

        public class PriceInfo
        {
            public decimal? eur { get; set; }
        }

        // Methode zum Stoppen des Timers, wenn die Anwendung heruntergefahren wird
        public void StopTimer()
        {
            lock (_lock)
            {
                _timer?.Dispose();
                _timer = null;
            }
        }
    }
}
