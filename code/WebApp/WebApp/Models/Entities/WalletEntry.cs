using WebApp.Components.Pages;

namespace WebApp.Models.Entities
{
    public class WalletEntry
    {
        public string Name { get; set; }
        public string Symbol { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Price { get; set; }
    }
}
