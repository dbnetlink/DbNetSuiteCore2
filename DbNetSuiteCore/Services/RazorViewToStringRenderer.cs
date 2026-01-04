using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class RazorViewToStringRenderer
{
    private readonly IRazorViewEngine _razorViewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public RazorViewToStringRenderer(
        IRazorViewEngine razorViewEngine,
        ITempDataProvider tempDataProvider,
        IServiceScopeFactory serviceScopeFactory)
    {
        _razorViewEngine = razorViewEngine;
        _tempDataProvider = tempDataProvider;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model, bool isMainPage = false)
    {
        var actionContext = GetActionContext();
        var view = FindView(actionContext, viewName, isMainPage);

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
                new TempDataDictionary(
                    actionContext.HttpContext,
                    _tempDataProvider),
                output,
                new HtmlHelperOptions());

            await view.RenderAsync(viewContext);

            return output.ToString();
        }
    }

    public async Task<string> RenderPageToStringAsync<TModel>(string viewName, TModel model)
    {
        return await RenderViewToStringAsync(viewName, model, true);
    }

    private IView FindView(ActionContext actionContext, string viewName, bool isMainPage)
    {
        var getViewResult = _razorViewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: isMainPage);
        if (getViewResult.Success)
        {
            return getViewResult.View;
        }

        var findViewResult = _razorViewEngine.FindView(actionContext, viewName, isMainPage: isMainPage);
        if (findViewResult.Success)
        {
            return findViewResult.View;
        }

        throw new FileNotFoundException($"Unable to find view '{viewName}'. Searched locations: {string.Join(Environment.NewLine, getViewResult.SearchedLocations)}");
    }

    private ActionContext GetActionContext()
    {
        var httpContext = new DefaultHttpContext { ServiceScopeFactory = _serviceScopeFactory };
        return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
    }
}