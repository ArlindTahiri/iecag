using System.ComponentModel.DataAnnotations;

namespace WebApp.Models.ViewModels
{
    public class TransactionModel
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Please select a cryptocurrency")]
        public string SelectedCryptocurrency { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Please provide an amount")]
        public double Amount { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Please provide a price")]
        public double AveragePrice { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Please provide a price")]
        public double PricePaidAll { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Please provide a transaction Date")]
        public DateTimeOffset TransactionDate { get; set; } = DateTimeOffset.Now;
    }
}
