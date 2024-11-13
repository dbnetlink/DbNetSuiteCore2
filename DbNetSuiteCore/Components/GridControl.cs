using Microsoft.AspNetCore.Html;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Enums;
using Newtonsoft.Json;

namespace DbNetSuiteCore
{
    public class GridControl : ComponentControl
    {
        public GridControl(HttpContext httpContext) : base(httpContext)
        {
        }

        public async Task<HtmlString> Render(GridModel gridModel)
        {
            if (gridModel.DataSourceType == DataSourceType.FileSystem)
            {
                gridModel._NestedGrids.Add(gridModel.DeepCopy());
            }

            ValidateControl(gridModel);

            return await base.RenderView("Grid/ControlForm", gridModel);
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
