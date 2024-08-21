using Microsoft.AspNetCore.Html;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore
{
    public class GridControl
    {
        private readonly HttpContext _httpContext;
        public GridControl(HttpContext httpContext) 
        {
            _httpContext = httpContext;
        }

        public async Task<HtmlString> Render(GridModel gridModel)
        {
            var viewRenderService = _httpContext.RequestServices.GetService<RazorViewToStringRenderer>();
            return new HtmlString(await viewRenderService!.RenderViewToStringAsync("GridControlForm", gridModel));
        }
    }
}
