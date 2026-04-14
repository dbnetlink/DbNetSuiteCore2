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
                return new HtmlString(await base.Render(gridModel));
            }

            if (componentModel is SelectModel selectModel)
            {
                return new HtmlString(await base.Render(selectModel));
            }

            if (componentModel is FormModel formModel)
            {
                return new HtmlString(await base.Render(formModel));
            }

            if (componentModel is TreeModel treeModel)
            {
                return new HtmlString(await base.Render(treeModel));
            }

            return new HtmlString(string.Empty);
        }

        public static Control Create(HttpContext httpContext)
        {
            return new Control(httpContext);
        }
    }
}
