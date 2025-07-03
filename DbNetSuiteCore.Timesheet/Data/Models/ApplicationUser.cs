using Microsoft.AspNetCore.Identity;

namespace DbNetSuiteCore.Timesheet.Data.Models
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime LastLogin { get; set; }
    }
}
