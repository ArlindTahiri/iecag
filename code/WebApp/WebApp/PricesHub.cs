using Microsoft.AspNetCore.SignalR;
using WebApp.Services;

public class PricesHub : Hub
{
    private readonly DataFetcherService _dataFetcherService;

    public PricesHub(DataFetcherService dataFetcherService)
    {
        _dataFetcherService = dataFetcherService;
    }

    public async Task SendPriceUpdate(decimal? price)
    {
        await Clients.All.SendAsync("ReceivePriceUpdate", price);
    }
}