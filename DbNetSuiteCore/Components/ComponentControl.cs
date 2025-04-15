using Microsoft.AspNetCore.Html;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Enums;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;

namespace DbNetSuiteCore
{
    public class ComponentControl
    {
        protected readonly HttpContext _httpContext;
        public ComponentControl(HttpContext httpContext)
        {
            _httpContext = httpContext;
        }

        public async Task<HtmlString> Render(GridModel gridModel)
        {
            if (gridModel.DataSourceType == DataSourceType.FileSystem)
            {
                gridModel._NestedGrids.Add(gridModel.DeepCopy());
            }

            ValidateControl(gridModel);

            return await RenderView("Grid/__ControlForm", gridModel);
        }

        public async Task<HtmlString> Render(SelectModel selectModel)
        {
            return await RenderView("Select/__ControlForm", selectModel);
        }

        public async Task<HtmlString> Render(FormModel formModel)
        {
            return await RenderView("Form/__ControlForm", formModel);
        }

        protected void ValidateControl(ComponentModel componentModel)
        {
            if (componentModel.DataSourceType == DataSourceType.FileSystem && componentModel.IsLinked)
            {
                componentModel.Url = string.Empty;
            }

            if (componentModel is GridModel)
            {
                GridModel gridModel = (GridModel)componentModel;

                if (gridModel.IsGrouped)
                {
                    gridModel.OptimizeForLargeDataset = false;
                }
            }
        }

        protected async Task<HtmlString> RenderView(string viewName, ComponentModel componentModel)
        {
            if (_httpContext == null)
            {
                return new HtmlString("<div style=\"padding:20px\"> An instance of HttpContext must be passed to the DbNetSuiteCore control constructor. This should be the <b>HttpContext</b> property in a Razor pages or the <b>Context</b> property in an MVC view e.g. </br><code>...</br> @(await new DbNetSuiteCore.Control(<b>HttpContext</b>).Render(customerGrid))</br>...</br> @(await new DbNetSuiteCore.Control(<b>Context</b>).Render(customerGrid))</br>...</code></div>");
            }

            var viewRenderService = _httpContext.RequestServices.GetService<RazorViewToStringRenderer>();
            return new HtmlString(await viewRenderService!.RenderViewToStringAsync(viewName, componentModel));
        }
    }

    public static class ExtensionMethods
    {
        public static T DeepCopy<T>(this T self)
        {
            var serialized = JsonConvert.SerializeObject(self);
            return JsonConvert.DeserializeObject<T>(serialized);
        }
    }
}
