using Microsoft.AspNetCore.Html;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore
{
    public class ComponentControl : BaseComponentControl
    {
        public ComponentControl(HttpContext httpContext) : base(httpContext)
        {
        }

        public new async Task<HtmlString> Render(GridModel gridModel)
        {
            return new HtmlString(await base.Render(gridModel));
        }

        public new async Task<HtmlString> Render(SelectModel selectModel)
        {
            return new HtmlString(await base.Render(selectModel));
        }

        public new async Task<HtmlString> Render(FormModel formModel)
        {
            return new HtmlString(await base.Render(formModel));
        }
    }
}
