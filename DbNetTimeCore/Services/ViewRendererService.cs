using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Services
{
    public class CustomViewRendererService
    {
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly ITempDataProvider _tempDataProvider;

        public CustomViewRendererService(
            IRazorViewEngine razorViewEngine,
            ITempDataProvider tempDataProvider)
        {
            _razorViewEngine = razorViewEngine;
            _tempDataProvider = tempDataProvider;
        }

        public async Task<string> RenderViewToStringAsync(ControllerContext actionContext, string viewPath, object model)
        {
            var viewEngineResult = _razorViewEngine.GetView(viewPath, viewPath, false);

            if (viewEngineResult.View == null || (!viewEngineResult.Success))
            {
                throw new ArgumentNullException($"Unable to find view '{viewPath}'");
            }

            var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), actionContext.ModelState);
            viewDictionary.Model = model;

            var view = viewEngineResult.View;
            var tempData = new TempDataDictionary(actionContext.HttpContext, _tempDataProvider);

            using var sw = new StringWriter();
            var viewContext = new ViewContext(actionContext, view, viewDictionary, tempData, sw, new HtmlHelperOptions());
            await view.RenderAsync(viewContext);
            return sw.ToString();
        }
    }
}