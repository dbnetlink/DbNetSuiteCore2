using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using System.Collections;

namespace DbNetSuiteCore.UnitTests.Plugins.JsonTransformPlugin
{
    public class JsonTransformPluginOtherException : JsonTransformPluginBase
    {
        public override IEnumerable Transform(GridModel gridModel)
        {
                throw new ArgumentException();
        }
    }
}
