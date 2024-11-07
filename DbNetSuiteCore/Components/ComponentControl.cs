using Microsoft.AspNetCore.Html;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore
{
    public class ComponentControl
    {
        protected readonly HttpContext _httpContext;
        public ComponentControl(HttpContext httpContext) 
        {
            _httpContext = httpContext;
        }

        protected async Task<HtmlString> RenderView(string viewName, ComponentModel componentModel)
        {
            var viewRenderService = _httpContext.RequestServices.GetService<RazorViewToStringRenderer>();
            return new HtmlString(await viewRenderService!.RenderViewToStringAsync(viewName, componentModel));
        }
    }
}
