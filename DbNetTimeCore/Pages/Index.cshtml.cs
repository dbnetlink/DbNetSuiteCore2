using DbNetTimeCore.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DbNetTimeCore.Pages
{
    public class IndexModel : PageModel
    {
        public DataGrid CustomersGrid { get; set; } = new DataGrid();
        public DataGrid FilmsGrid { get; set; } = new DataGrid();
        public DataGrid ActorsGrid { get; set; } = new DataGrid();
    }
}
