using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using System.Collections;

namespace DbNetSuiteCore.UnitTests.Plugins.JsonTransformPlugin
{
    public abstract class JsonTransformPluginBase : IJsonTransformPlugin
    {
        public static List<string> Items { get; set; } = new List<string>() { "AAAAA", "BBBBB", "CCCCC" };

        public abstract IEnumerable Transform(GridModel gridModel);
    }
}
