using DbNetTimeCore.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DbNetTimeCore.Pages
{
    public class IndexModel : PageModel
    {
        public GridViewModel CustomersGrid { get; set; }
        public GridViewModel FilmsGrid { get; set; }
        public GridViewModel ActorsGrid { get; set; }
    }
}
