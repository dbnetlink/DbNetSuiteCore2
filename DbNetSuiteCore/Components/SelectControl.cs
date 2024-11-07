using Microsoft.AspNetCore.Html;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore
{
    public class SelectControl : ComponentControl
    {
        public SelectControl(HttpContext httpContext): base(httpContext)
        {
        }

        public async Task<HtmlString> Render(SelectModel selectModel)
        {
            return await RenderView("Select/ControlForm", selectModel);
        }
    }
}
