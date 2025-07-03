using DbNetSuiteCore.Timesheet.Data.Models;
using DbNetSuiteCore.Timesheet.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DbNetSuiteCore.Timesheet.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager) : base(logger, userManager)
        {
        }

        public IActionResult Index()
        {
            return View(BaseViewModel);
        }
    }
}
