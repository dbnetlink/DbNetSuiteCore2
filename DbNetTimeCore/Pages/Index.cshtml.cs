using DbNetTimeCore.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DbNetTimeCore.Pages
{
    public class IndexModel : PageModel
    {
        public DataGrid DataGrid { get; set; } = new DataGrid();
    }
}
