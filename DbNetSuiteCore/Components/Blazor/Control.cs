using Microsoft.AspNetCore.Components;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.Blazor
{
    public class Control : BaseComponentControl
    {
        public Control(HttpContext httpContext): base(httpContext)
        {
        }

        public Control(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }

        public MarkupString Render(ComponentModel componentModel)
        {
            componentModel.IsBlazor = true;
            if (componentModel is GridModel gridModel)
            {
                return new MarkupString(base.Render(gridModel).Result);
            }

            if (componentModel is SelectModel selectModel)
            {
                return new MarkupString(base.Render(selectModel).Result);
            }

            if (componentModel is FormModel formModel)
            {
                return new MarkupString(base.Render(formModel).Result);
            }

            return new MarkupString(string.Empty);
        }
    }
}
