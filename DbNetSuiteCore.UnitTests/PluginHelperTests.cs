using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.UnitTests.Plugins;
using Newtonsoft.Json;


namespace DbNetSuiteCore.UnitTests
{
    public class PluginHelperTests
    {
        JsonTransformPluginNotImplemented JsonTransformPluginNotImplemented = new JsonTransformPluginNotImplemented() { Items = new List<string>() { "AAAAA", "BBBBB", "CCCCC" } };

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void HandleJsonTransformPluginNotImplemented()
        {
            string json = JsonConvert.SerializeObject(JsonTransformPluginNotImplemented);
            string transformedJson = PluginHelper.TransformJson(json, typeof(JsonTransformPluginNotImplemented), new GridModel());

            Assert.True(json == transformedJson);
        }
    }
}