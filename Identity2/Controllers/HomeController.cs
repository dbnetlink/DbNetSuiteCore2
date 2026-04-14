using DbNetSuiteCore.Identity.Constants;
using DbNetSuiteCore.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DbNetSuiteCore.Identity.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager) : base(logger, userManager)
        {
        }

        public async Task<IActionResult> Index()
        {
            if ((await _userManager.GetUsersInRoleAsync(Roles.Admin)).Any() == false)
            {
                return Redirect("/register");
            }

            return View(BaseViewModel);
        }
    }
}
