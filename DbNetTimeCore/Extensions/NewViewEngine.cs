using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Reflection;

namespace DbNetTimeCore.Extensions
{
    public class EmbeddedViewEngine : IViewEngine
    {
        private readonly Assembly _viewAssembly;

        public EmbeddedViewEngine()
        {
            _viewAssembly = typeof(EmbeddedViewEngine).Assembly;
        }

        public ViewEngineResult FindView(ActionContext context, string viewName, bool isMainPage)
        {
            var resourceName = $"{_viewAssembly.GetName().Name}.Views.{viewName}.cshtml";

            if (_viewAssembly.GetManifestResourceInfo(resourceName) != null)
            {
                return ViewEngineResult.Found(viewName, new EmbeddedView(_viewAssembly, resourceName));
            }

            return ViewEngineResult.NotFound(viewName, new[] { resourceName });
        }

        public ViewEngineResult GetView(string executingFilePath, string viewPath, bool isMainPage)
        {
            return ViewEngineResult.NotFound(viewPath, new[] { viewPath });
        }
    }

    public class EmbeddedView : IView
    {
        private readonly Assembly _assembly;
        private readonly string _resourceName;

        public EmbeddedView(Assembly assembly, string resourceName)
        {
            _assembly = assembly;
            _resourceName = resourceName;
        }

        public string Path => _resourceName;

        public async Task RenderAsync(ViewContext context)
        {
            using (var stream = _assembly.GetManifestResourceStream(_resourceName))
            using (var reader = new StreamReader(stream))
            {
                var content = await reader.ReadToEndAsync();
                await context.Writer.WriteAsync(content);
            }
        }
    }



    public static class ViewRendererExtensions
    {
        public static async Task<string> RenderViewToStringAsync2<TModel>(PageModel pageModel, string viewName, TModel model)
        {
            var actionContext = new ActionContext(
                pageModel.HttpContext,
                pageModel.RouteData,
                pageModel.PageContext.ActionDescriptor
            );

            var viewEngine = new EmbeddedViewEngine();
            var viewEngineResult = viewEngine.FindView(actionContext, viewName, false);
            var view = viewEngineResult.View;

            using (var output = new StringWriter())
            {
                var viewContext = new ViewContext(
                    actionContext,
                    view,
                    new ViewDataDictionary<TModel>(
                        metadataProvider: new EmptyModelMetadataProvider(),
                        modelState: new ModelStateDictionary())
                    {
                        Model = model
                    },
                    pageModel.TempData,
                    output,
                    new HtmlHelperOptions());

                await view.RenderAsync(viewContext);

                return output.ToString();
            }
        }

        public static async Task<string> RenderViewToStringAsync(PageModel pageModel, string viewName,  object model = null)
        {
            if (string.IsNullOrEmpty(viewName))
                throw new ArgumentException("View name cannot be null or empty", nameof(viewName));

            var actionContext = new ActionContext(
                pageModel.HttpContext,
                pageModel.RouteData,
                pageModel.PageContext.ActionDescriptor
            );

            using (var writer = new StringWriter())
            {
                var viewEngine = new EmbeddedViewEngine();
                var viewResult = viewEngine.FindView(actionContext, viewName, false);

                if (!viewResult.Success)
                {
                    throw new Exception($"A view with the name {viewName} could not be found");
                }

                var viewData = new ViewDataDictionary(pageModel.ViewData)
                {
                    Model = model
                };

                var viewContext = new ViewContext(
                    actionContext,
                    viewResult.View,
                    viewData,
                    pageModel.TempData,
                    writer,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);

                return writer.GetStringBuilder().ToString();
            }
        }
    }
}
