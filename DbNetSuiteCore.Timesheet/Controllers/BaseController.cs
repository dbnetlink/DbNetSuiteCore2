using DbNetSuiteCore.Timesheet.Data.Models;
using DbNetSuiteCore.Timesheet.Models;
using DbNetSuiteCore.Timesheet.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DbNetSuiteCore.Timesheet.Controllers
{
    public class BaseController : Controller
    {
        protected readonly ILogger<HomeController> _logger;
        protected readonly UserManager<ApplicationUser> _userManager;

        public BaseViewModel BaseViewModel { get; set; } = new BaseViewModel();

        public BaseController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
            BaseViewModel.userManager = userManager;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
