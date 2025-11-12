using Microsoft.AspNetCore.Identity;

namespace DbNetSuiteCore.Timesheet.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string? FullName { get; set; }

    }
}