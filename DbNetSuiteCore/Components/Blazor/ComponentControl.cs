using Microsoft.AspNetCore.Components;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.Blazor
{
    public class ComponentControl : BaseComponentControl
    {
        public ComponentControl(HttpContext httpContext) : base(httpContext)
        {
        }

        public new async Task<MarkupString> Render(GridModel gridModel)
        {
            return new MarkupString(await base.Render(gridModel));
        }

        public new async Task<MarkupString> Render(SelectModel selectModel)
        {
            return new MarkupString(await base.Render(selectModel));
        }

        public new async Task<MarkupString> Render(FormModel formModel)
        {
            return new MarkupString(await base.Render(formModel));
        }
    }
}
