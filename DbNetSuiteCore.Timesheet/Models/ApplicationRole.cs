using Microsoft.AspNetCore.Identity;

namespace DbNetSuiteCore.Timesheet.Models
{
    public class ApplicationRole : IdentityRole<Guid>
    {
        public string? Description { get; set; }
    }
}
