using Microsoft.AspNetCore.Components;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.Blazor
{
    public class Control : ComponentControl
    {
        public Control(HttpContext httpContext): base(httpContext)
        {
        }

        public async Task<MarkupString> Render(ComponentModel componentModel)
        {
            componentModel.IsBlazor = true;
            if (componentModel is GridModel)
            {
                return await base.Render((GridModel)componentModel);
            }

            if (componentModel is SelectModel)
            {
                return await base.Render((SelectModel)componentModel);
            }

            if (componentModel is FormModel)
            {
                return await base.Render((FormModel)componentModel);
            }

            return new MarkupString(string.Empty);
        }
    }
}
