using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using RazorEngineCore;
using System;
using System.Reflection;


namespace Microsoft.AspNetCore.Mvc
{
    public static class DbNetLink
    {

        public static async Task<string> RenderRazorToString<TModel>(string embeddedResourcePath, TModel model)
        {
            var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();

            IRazorEngine razorEngine = new RazorEngine();
            string templateText = File.ReadAllText(ReadEmbeddedResource(embeddedResourcePath));

            IRazorEngineCompiledTemplate template2 = await razorEngine.CompileAsync(templateText);

            return template2.Run(model);
        }

        private static string ReadEmbeddedResource(string resourcePath)
        {
            // Implement logic to read content from embedded resource based on your project structure
            // This example assumes Assembly is the current assembly
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static async Task<string> RenderToString(this HttpContext httpContext, string viewName, object model)
        {
            var viewEngine = httpContext.RequestServices.GetRequiredService<IRazorViewEngine>();
            var viewOptions = httpContext.RequestServices.GetRequiredService<IOptions<MvcViewOptions>>();
            var engine = new DbNetTimeCore.Extensions.RazorEngine(viewEngine, viewOptions);
            return await engine.RenderAsync(httpContext, viewName, model);
        }

        public static async Task<string> RenderViewAsync(this HttpContext httpContext, string viewName, PageModel model, bool partial = false)
        {
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor());
            var controllerContext = new ControllerContext(actionContext);

            var tempDataService = httpContext.RequestServices.GetRequiredService<ITempDataDictionary>();

            using (var writer = new StringWriter())
            {
                IViewEngine viewEngine = httpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
                ViewEngineResult viewResult = viewEngine.FindView(controllerContext, viewName, !partial);

                if (viewResult.Success == false)
                {
                    return $"A view with the name {viewName} could not be found";
                }

                ViewContext viewContext = new ViewContext(
                    controllerContext,
                    viewResult.View,
                    new ViewDataDictionary(new EmptyModelMetadataProvider(), controllerContext.ModelState),
                    tempDataService,
                    writer,
                    new HtmlHelperOptions()
                );

                viewContext.ViewData.Model = model;

                await viewResult.View.RenderAsync(viewContext);

                return writer.GetStringBuilder().ToString();
            }
        }
    }
}
