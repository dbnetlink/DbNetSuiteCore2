using Microsoft.AspNetCore.Mvc;

namespace DbNetSuiteCore.Web.Controllers
{
    public class HomeController : Controller
    {
        public HomeController()
        {
        }

        public IActionResult Index()
        {
            return View();
        }
     
    }
}
