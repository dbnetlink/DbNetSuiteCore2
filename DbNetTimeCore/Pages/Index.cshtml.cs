using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;

namespace DbNetTimeCore.Pages
{
    public class IndexModel : PageModel
    {
        public DataTable Data { get; set; } = new DataTable();

        public string Test => "TEST";
    }
}
