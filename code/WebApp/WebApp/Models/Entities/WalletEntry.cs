using WebApp.Components.Pages;

namespace WebApp.Models.Entities
{
    public class WalletEntry
    {
        public string Name { get; set; }
        public decimal? Amount { get; set; }
        public decimal? AveragePrice { get; set; }
        public decimal? CurrentPrice { get; set; }
    }
}
