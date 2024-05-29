using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc
{
    public static class DbNetLink
    {
        public static async Task<string> RenderToString(this HttpContext httpContext, string viewName, object model)
        {
            var viewEngine = httpContext.RequestServices.GetRequiredService<IRazorViewEngine>();
            var viewOptions = httpContext.RequestServices.GetRequiredService<IOptions<MvcViewOptions>>();
            var engine = new DbNetTimeCore.Extensions.RazorEngine(viewEngine, viewOptions);
            return await engine.RenderAsync(httpContext, viewName, model);
        }
    }
}