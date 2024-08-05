using TQ.Models;

namespace TQ.Components
{
    public class GridControl
    {
        private readonly HttpContext _httpContext;
        public GridControl(HttpContext httpContext) 
        {
            _httpContext = httpContext;
        }
        public string ConnectionAlias { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;

        public async Task<string> Render(GridModel gridComponentModel)
        {
            var viewRenderService = _httpContext.RequestServices.GetService<RazorViewToStringRenderer>();
            return await viewRenderService!.RenderViewToStringAsync("_gridControlForm", gridComponentModel);
        }
    }
}
