using Microsoft.AspNetCore.SignalR;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
                FetchPriceFromBackend("https://api.coingecko.com/api/v3/coins/crypto-com-chain", "CRO");
                FetchPriceFromBackend("https://api.coingecko.com/api/v3/coins/bitcoin", "BTC");
                FetchPriceFromBackend("https://api.coingecko.com/api/v3/coins/ethereum", "ETH");
            }
        }

        public async Task FetchPriceFromBackend(string url, string symbol)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                

                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    var obj = JsonSerializer.Deserialize<CoinGeckoResponse>(responseData);
                    decimal? price = obj?.market_data?.current_price?.eur;
                    await _hubContext.Clients.All.SendAsync("ReceivePriceUpdate", symbol, price);
                }
                else
                {
                    Random random = new Random();
                    await _hubContext.Clients.All.SendAsync("ReceivePriceUpdate", symbol, random.Next(5,1000));
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
