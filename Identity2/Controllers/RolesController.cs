using DbNetSuiteCore.Identity.Constants;
using DbNetSuiteCore.Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DbNetSuiteCore.Identity.Controllers
{
    public class RolesController : BaseController
    {
        public RolesController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager) : base(logger, userManager)
        {
        }
        [Authorize(Roles = Roles.Admin)]
        public IActionResult Index()
        {
            return View(BaseViewModel);
        }
    }
}
