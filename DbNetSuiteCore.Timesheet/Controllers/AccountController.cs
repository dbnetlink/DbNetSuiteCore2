using DbNetSuiteCore.Timesheet.Data.Models;
using DbNetSuiteCore.Timesheet.Models;
using DbNetSuiteCore.Timesheet.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DbNetSuiteCore.Timesheet.Controllers
{
    public class AccountController : BaseController
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        public AccountController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : base(logger, userManager)
        {   
            _signInManager = signInManager;
        }
        [HttpGet]
        [Route("login")]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }


        [HttpPost]
        [Route("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, true, lockoutOnFailure: false);

            if (result.Succeeded)
                return Redirect("/");

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }
    }
}
