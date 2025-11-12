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
        private readonly UserManager<ApplicationUser> _userManager;
        public AccountController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : base(logger, userManager)
        {   
            _signInManager = signInManager;
            _userManager = userManager;
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

        [HttpGet]
        [Route("register")]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }


        [HttpPost]
        [Route("register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _userManager.CreateAsync(new ApplicationUser() { Email = model.Email, UserName = model.Email, EmailConfirmed = true, AccessFailedCount = 0 }, model.Password);

            if (result.Succeeded)
                return Redirect("/login");

            ModelState.AddModelError(string.Empty, "Creation of admin user was not successful.");
            return View(model);
        }
    }
}
