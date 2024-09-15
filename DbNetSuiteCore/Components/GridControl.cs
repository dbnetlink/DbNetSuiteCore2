using Microsoft.AspNetCore.Html;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Enums;
using Newtonsoft.Json;

namespace DbNetSuiteCore
{
    public class GridControl
    {
        private readonly HttpContext _httpContext;
        public GridControl(HttpContext httpContext) 
        {
            _httpContext = httpContext;
        }

        public async Task<HtmlString> Render(GridModel gridModel)
        {
            if (gridModel.DataSourceType == DataSourceType.FileSystem)
            {
                gridModel.NestedGrid = gridModel.DeepCopy();
            }
            var viewRenderService = _httpContext.RequestServices.GetService<RazorViewToStringRenderer>();
            return new HtmlString(await viewRenderService!.RenderViewToStringAsync("Grid/ControlForm", gridModel));
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
