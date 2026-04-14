using Microsoft.AspNetCore.Identity;

namespace DbNetSuiteCore.Identity.Models
{
    public class ApplicationRole : IdentityRole<Guid>
    {
        public string? Description { get; set; }
    }
}
