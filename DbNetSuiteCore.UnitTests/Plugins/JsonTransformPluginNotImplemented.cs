using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using System.Collections;

namespace DbNetSuiteCore.UnitTests.Plugins
{
    public class JsonTransformPluginNotImplemented : IJsonTransformPlugin
    {
        public List<string>? Items { get; set; }
        public IEnumerable Transform(GridModel gridModel)
        {
            throw new NotImplementedException();
        }
    }
}
