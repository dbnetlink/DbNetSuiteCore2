using Microsoft.AspNetCore.Html;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Enums;
using Newtonsoft.Json;

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

            return await RenderView("Grid/ControlForm", gridModel);
        }

        public async Task<HtmlString> Render(SelectModel selectModel)
        {
            return await RenderView("Select/ControlForm", selectModel);
        }

        public async Task<HtmlString> Render(FormModel formModel)
        {
            return await RenderView("Form/ControlForm", formModel);
        }

        protected void ValidateControl(ComponentModel componentModel)
        {
            if (componentModel.DataSourceType == DataSourceType.FileSystem && componentModel.IsLinked)
            {
                componentModel.Url = string.Empty;
            }
        }

        protected async Task<HtmlString> RenderView(string viewName, ComponentModel componentModel)
        {
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
