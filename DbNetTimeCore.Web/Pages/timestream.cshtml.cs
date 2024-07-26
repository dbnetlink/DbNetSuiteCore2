using DbNetTimeCore.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DbNetTimeCore.Pages
{
    public class TimestreamModel : PageModel
    {
        public GridViewModel MT2Grid { get; set; }
        public GridViewModel MT1Grid { get; set; }
    }
}
