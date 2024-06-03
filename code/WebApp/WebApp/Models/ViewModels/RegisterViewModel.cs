using System.ComponentModel.DataAnnotations;

namespace WebApp.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Please provide User Name")]
        public string? UserName { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Please provide Password")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Please confirm Password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and Confirm Password do not match")]
        public string? ConfirmPassword { get; set; }
    }
}