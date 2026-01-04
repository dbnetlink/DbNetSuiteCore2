using Microsoft.AspNetCore.Identity;

namespace DbNetSuiteCore.Identity.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string? FullName { get; set; }

    }
}