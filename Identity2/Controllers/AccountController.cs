using DbNetSuiteCore.Identity.Constants;
using DbNetSuiteCore.Identity.Models;
using DbNetSuiteCore.Identity.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DbNetSuiteCore.Identity.Controllers
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

            if (!result.Succeeded)
            {
                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            if ((await _userManager.GetUsersInRoleAsync(Roles.Admin)).Any() == false)
            {
                var user = await _userManager.FindByNameAsync(model.Email);
                if (user != null)
                {
                    await _userManager.AddToRolesAsync(user, new string[] { Roles.Admin });
                }
                return Redirect("/login");
            }

            return Redirect("/login");
        }

        [HttpGet]
        [Route("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();            
            return Redirect("/");
        }
    }
}
