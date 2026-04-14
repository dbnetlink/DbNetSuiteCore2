using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DbNetSuiteCore.Web.Pages.GridControl.Json
{
    [IgnoreAntiforgeryToken]
    public class FoodStandardsApiModel : PageModel
    {
        protected IConfiguration configuration;
        protected IWebHostEnvironment? env;
        public Dictionary<string, string> EndPoints { get; set; } = new Dictionary<string, string>();
        [BindProperty]
        public string EndPoint { get; set; } = string.Empty;
        public string EndPointName => EndPoints.FirstOrDefault(kvp => kvp.Value == EndPoint).Key ?? string.Empty;

        public FoodStandardsApiModel(IConfiguration configuration, IWebHostEnvironment env)
        {
            this.configuration = configuration;
            this.env = env;

            EndPoints = new Dictionary<string, string>()
            {
                {"Business Types","BusinessTypes" },
                {"Establishments","Establishments/basic"},
                {"Authorities","Authorities/basic"}
            };
        }
    }
}
