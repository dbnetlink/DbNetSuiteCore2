using Microsoft.AspNetCore.Html;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Enums;
using Newtonsoft.Json;

namespace DbNetSuiteCore
{
    public class BaseComponentControl
    {
        protected readonly HttpContext _httpContext;
        public BaseComponentControl(HttpContext httpContext)
        {
            _httpContext = httpContext;
        }

        protected async Task<string> Render(GridModel gridModel)
        {
            if (gridModel.DataSourceType == DataSourceType.FileSystem)
            {
                gridModel._NestedGrids.Add(gridModel.DeepCopy());
            }

            ValidateControl(gridModel);

            return await RenderView("Grid/__ControlForm", gridModel);
        }

        protected async Task<string> Render(SelectModel selectModel)
        {
            return await RenderView("Select/__ControlForm", selectModel);
        }

        protected async Task<string> Render(FormModel formModel)
        {
            return await RenderView("Form/__ControlForm", formModel);
        }

        private void ValidateControl(ComponentModel componentModel)
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

                if (gridModel.RowSelection == RowSelection.None)
                {
                    if (gridModel.IsParent || gridModel.ViewDialog != null || gridModel.ClientEvents.ContainsKey(GridClientEvent.RowSelected))
                    {
                        gridModel.RowSelection = RowSelection.Single;
                    }
                }
            }
        }

        protected async Task<string> RenderView(string viewName, ComponentModel componentModel)
        {
            if (_httpContext == null)
            {
                return "<div style=\"padding:20px\"> An instance of HttpContext must be passed to the DbNetSuiteCore control constructor. This should be the <b>HttpContext</b> property in a Razor pages or the <b>Context</b> property in an MVC view e.g. </br><code>...</br> @(await new DbNetSuiteCore.Control(<b>HttpContext</b>).Render(customerGrid))</br>...</br> @(await new DbNetSuiteCore.Control(<b>Context</b>).Render(customerGrid))</br>...</code></div>";
            }

            var viewRenderService = _httpContext.RequestServices.GetService<RazorViewToStringRenderer>();
            return await viewRenderService!.RenderViewToStringAsync(viewName, componentModel);
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
