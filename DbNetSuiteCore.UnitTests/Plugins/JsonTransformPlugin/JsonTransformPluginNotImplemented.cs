using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using System.Collections;

namespace DbNetSuiteCore.UnitTests.Plugins.JsonTransformPlugin
{
    public class JsonTransformPluginNotImplemented : JsonTransformPluginBase
    {
        public override IEnumerable Transform(GridModel gridModel)
        {
            throw new NotImplementedException();
        }
    }
}
