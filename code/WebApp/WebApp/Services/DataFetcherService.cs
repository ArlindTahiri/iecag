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

        private readonly TableClient _tableClientCurrentPrices;
        private readonly TableClient _tableClientPriceHistory7Days;
        private readonly TableClient _tableClientPriceHistory30Days;
        private readonly TableClient _tableClientPriceHistory180Days;


        public DataFetcherService(HttpClient httpClient, IHubContext<PricesHub> hubContext, string connectionString)
        {
            var serviceClient = new TableServiceClient(connectionString);
            _tableClientCurrentPrices = serviceClient.GetTableClient("currentprices");
            _tableClientCurrentPrices.CreateIfNotExists();
            _tableClientPriceHistory7Days = serviceClient.GetTableClient("pricehistory7days");
            _tableClientPriceHistory7Days.CreateIfNotExists();
            _tableClientPriceHistory30Days = serviceClient.GetTableClient("pricehistory30days");
            _tableClientPriceHistory30Days.CreateIfNotExists();
            _tableClientPriceHistory180Days = serviceClient.GetTableClient("pricehistory180days");
            _tableClientPriceHistory180Days.CreateIfNotExists();

            _httpClient = httpClient;
            _hubContext = hubContext;
            _timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        private async void TimerCallback(object state)
        {
            lock (_lock)
            {
                // Code to fetch data from backend
                FetchCurrentPricesFromAzureTable();
                /*
                FetchPriceFromBackend("https://api.coingecko.com/api/v3/coins/crypto-com-chain", "Cronos");
                FetchPriceFromBackend("https://api.coingecko.com/api/v3/coins/bitcoin", "Bitcoin");
                FetchPriceFromBackend("https://api.coingecko.com/api/v3/coins/ethereum", "Ethereum");
                */
            }
        }

        
        public async Task FetchCurrentPricesFromAzureTable()
        {
            // Load all current prices from the Azure Table and send them to all clients
            await foreach (var entity in _tableClientCurrentPrices.QueryAsync<TableEntity>())
            {
                await _hubContext.Clients.All.SendAsync("ReceivePriceUpdate", entity.PartitionKey, entity["price"]);
            }
        }


        public async Task<List<CoinPrice>> FetchAllCurrentPrices()
        {
           List<CoinPrice> coinPrices = new List<CoinPrice>();
            await foreach (var entity in _tableClientCurrentPrices.QueryAsync<CoinPrice>())
            {
                coinPrices.Add(entity);
            }
            return coinPrices;
        }
        
        public async Task<double> FetchCurrentPrice(string coin)
        {
            var entity = await _tableClientCurrentPrices.GetEntityAsync<CoinPrice>(coin, "");
            if (entity.HasValue)
            {
                var coinPrice = entity.Value;
                return coinPrice.price;
            }
            else
            {
                return 0;
            }
        }
        

        public async Task<List<KeyValuePair<DateTime, double>>> FetchPriceOfLast180Days(string coin)
        {
            List<KeyValuePair<DateTime, double>> prices = new List<KeyValuePair<DateTime, double>>();
            await foreach (var entity in _tableClientPriceHistory180Days.QueryAsync<CoinPrice>($"PartitionKey eq '{coin}'"))
            {
                prices.Add(new KeyValuePair<DateTime, double>(entity.PriceDate.DateTime, entity.price));
            }
            prices = prices.OrderBy(x => x.Key).ToList();
            return prices;
        }

        public async Task<List<KeyValuePair<DateTime, double>>> FetchPriceOfLast30Days(string coin)
        {
            List<KeyValuePair<DateTime, double>> prices = new List<KeyValuePair<DateTime, double>>();
            await foreach (var entity in _tableClientPriceHistory30Days.QueryAsync<CoinPrice>($"PartitionKey eq '{coin}'"))
            {
                prices.Add(new KeyValuePair<DateTime, double>(entity.PriceDate.DateTime, entity.price));
            }
            prices = prices.OrderBy(x => x.Key).ToList();
            return prices;
        }

        public async Task<List<KeyValuePair<DateTime, double>>> FetchPriceOfLast7Days(string coin)
        {
            List<KeyValuePair<DateTime, double>> prices = new List<KeyValuePair<DateTime, double>>();
            await foreach (var entity in _tableClientPriceHistory7Days.QueryAsync<CoinPrice>($"PartitionKey eq '{coin}'"))
            {
                prices.Add(new KeyValuePair<DateTime, double>(entity.PriceDate.DateTime, entity.price));
            }

            prices = prices.OrderBy(x => x.Key).ToList();
            return prices;
        }

        public async Task<List<KeyValuePair<DateTime, double>>> FetchPriceOfLastXDays(string coin, int days)
        {
            if (days == 7)
            {
                return await FetchPriceOfLast7Days(coin);
            }
            else if (days == 30)
            {
                return await FetchPriceOfLast30Days(coin);
            }
            else if (days == 180)
            {
                return await FetchPriceOfLast180Days(coin);
            }
            else
            {
                return new List<KeyValuePair<DateTime, double>>();
            }
        }



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

        // Methode to stop the timer when the application is stopped
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
