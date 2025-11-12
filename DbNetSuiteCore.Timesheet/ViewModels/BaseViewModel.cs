using DbNetSuiteCore.Timesheet.Constants;
using DbNetSuiteCore.Timesheet.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace DbNetSuiteCore.Timesheet.ViewModels
{
    public class BaseViewModel
    {
        public UserManager<ApplicationUser>? userManager { get; set; }  
        public List<string> UserRoles { get; set; } = new List<string>();
        public BaseViewModel() { }

        public bool IsAdministrator(ClaimsPrincipal user)
        {
            if ((user.Identity?.IsAuthenticated ?? false) && userManager != null)
            {
                var currentUser = userManager.GetUserAsync(user).Result;
                UserRoles = userManager.GetRolesAsync(currentUser!).Result.ToList();
            }

            return UserRoles.Contains(Roles.Administrator);
        }
    }
}
