using DbNetSuiteCore.Models;
using System.Collections;

namespace DbNetSuiteCore.UnitTests.Plugins.JsonTransformPlugin
{
    public class JsonTransformPluginSortDesc : JsonTransformPluginBase
    {
        public override IEnumerable Transform(GridModel gridModel)
        {
            return Items!.OrderByDescending(i => i).ToList();
        }
    }
}
