using Microsoft.AspNetCore.Html;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore
{
    public class Control : BaseComponentControl
    {
        public Control(HttpContext httpContext): base(httpContext)
        {
        }

        public async Task<HtmlString> Render(ComponentModel componentModel)
        {
            if (componentModel is GridModel gridModel)
            {
                return new HtmlString(await base.Render((GridModel)componentModel));
            }

            if (componentModel is SelectModel selectModel)
            {
                return new HtmlString(await base.Render((SelectModel)componentModel));
            }

            if (componentModel is FormModel formModel)
            {
                return new HtmlString(await base.Render((FormModel)componentModel));
            }

            return new HtmlString(string.Empty);
        }

        public static Control Create(HttpContext httpContext)
        {
            return new Control(httpContext);
        }
    }
}
