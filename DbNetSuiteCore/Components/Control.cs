using Microsoft.AspNetCore.Html;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore
{
    public class Control : ComponentControl
    {
        public Control(HttpContext httpContext): base(httpContext)
        {
        }

        public async Task<HtmlString> Render(ComponentModel componentModel)
        {
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

            return new HtmlString(string.Empty);
        }
    }
}
