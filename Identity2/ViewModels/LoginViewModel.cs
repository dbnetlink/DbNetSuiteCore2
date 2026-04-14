using System.ComponentModel.DataAnnotations;

namespace DbNetSuiteCore.Identity.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public LoginViewModel() : base()
        { 
        }
    }
}
