using System.ComponentModel.DataAnnotations;

namespace DbNetSuiteCore.Identity.ViewModels
{
    public class RegisterViewModel : BaseViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public RegisterViewModel() : base()
        { 
        }
    }
}
