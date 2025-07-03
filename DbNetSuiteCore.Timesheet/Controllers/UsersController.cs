using DbNetSuiteCore.Timesheet.Constants;
using DbNetSuiteCore.Timesheet.Data.Models;
using DbNetSuiteCore.Timesheet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DbNetSuiteCore.Timesheet.Controllers
{
    public class UsersController : BaseController
    {
        public UsersController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager) : base(logger, userManager)
        {
        }

        [Authorize(Roles = Roles.Administrator)]
        public IActionResult Index()
        {
            return View(BaseViewModel);
        }
    }
}
