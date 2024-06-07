using WebApp.Components.Pages;

namespace WebApp.Models.Entities
{
    public class WalletEntry
    {
        public string Name { get; set; }
        public double Amount { get; set; }
        public double AveragePrice { get; set; }
        public double CurrentPrice { get; set; }
    }
}
