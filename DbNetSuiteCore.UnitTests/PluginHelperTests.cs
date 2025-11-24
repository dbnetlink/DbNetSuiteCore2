using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using DbNetSuiteCore.UnitTests.Plugins.CustomFormPlugin;
using DbNetSuiteCore.UnitTests.Plugins.CustomGridPlugin;
using DbNetSuiteCore.UnitTests.Plugins.JsonTransformPlugin;
using Newtonsoft.Json;
using System.Data;


namespace DbNetSuiteCore.UnitTests
{
    public class PluginHelperTests
    {

        private List<string> ICustomGridPluginMethodNames = new List<string>
        {
            nameof(ICustomGridPlugin.Initialisation),
            nameof(ICustomGridPlugin.ValidateUpdate),
            nameof(ICustomGridPlugin.TransformDataTable)
        };

        private List<string> ICustomFormPluginMethodNames = new List<string>
        {
            nameof(ICustomFormPlugin.Initialisation),
            nameof(ICustomFormPlugin.ValidateUpdate),
            nameof(ICustomFormPlugin.ValidateInsert),
            nameof(ICustomFormPlugin.ValidateDelete),
            nameof(ICustomFormPlugin.CustomCommit)
        };

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void JsonTransformPluginNotImplementedTest()
        {
            string json = JsonConvert.SerializeObject(new JsonTransformPluginNotImplemented());
            string transformedJson = PluginHelper.TransformJson(new GridModel() { JsonTransformPlugin = typeof(JsonTransformPluginNotImplemented) }, json);
            Assert.True(json == transformedJson);
        }

        [Test]
        public void JsonTransformPluginOtherExceptionTest()
        {
            string json = JsonConvert.SerializeObject(new JsonTransformPluginOtherException());

            try
            {
                string transformedJson = PluginHelper.TransformJson(new GridModel() { JsonTransformPlugin = typeof(JsonTransformPluginNotImplemented) }, json);
            }
            catch (Exception ex)
            {
                Assert.True(ex is ArgumentException);
            }
        }


        [Test]
        public void JsonTransformPluginSortDescTest()
        {
            JsonTransformPluginSortDesc jsonTransformPluginSortDesc = new JsonTransformPluginSortDesc();

            string json = JsonConvert.SerializeObject(jsonTransformPluginSortDesc);
            string transformedJson = PluginHelper.TransformJson(new GridModel() { JsonTransformPlugin = typeof(JsonTransformPluginSortDesc) }, json);

            json = JsonConvert.SerializeObject(jsonTransformPluginSortDesc.Items!.OrderByDescending(i => i).ToList());
            Assert.True(json == transformedJson);
        }

        [Test]
        public void CustomGridPluginNotImplementedTest()
        {
            foreach (string methodName in ICustomGridPluginMethodNames)
            {
                object? returnValue = PluginHelper.InvokeMethod(typeof(CustomGridPluginNotImplemented), methodName, new GridModel(), null);
                Assert.IsNull(returnValue);
            }
        }

        [Test]
        public void CustomGridPluginExceptionTest()
        {
            foreach (string methodName in ICustomGridPluginMethodNames)
            {
                GridModel gridModel = new GridModel();
                PluginHelper.InvokeMethod(typeof(CustomGridPluginException), methodName, gridModel);
                Assert.IsTrue(gridModel.Message == CustomGridPluginBase.TestExceptionMessage);
            }
        }

        [Test]
        public void CustomGridPluginTest()
        {
            foreach (string methodName in ICustomGridPluginMethodNames)
            {
                GridModel gridModel = new GridModel();
                switch (methodName)
                {
                    case nameof(ICustomGridPlugin.Initialisation):
                        PluginHelper.InvokeMethod(typeof(CustomGridPlugin), methodName, gridModel);
                        Assert.IsTrue(gridModel.Columns.Count() == 1);
                        Assert.IsTrue(gridModel.Columns.First().Expression == "TEST");
                        break;
                    case nameof(ICustomGridPlugin.ValidateUpdate):
                        bool? returnValue = (bool?)PluginHelper.InvokeMethod(typeof(CustomGridPlugin), methodName, gridModel);
                        Assert.IsTrue(returnValue.HasValue && returnValue.Value);
                        break;
                    case nameof(ICustomGridPlugin.TransformDataTable):
                        for (int c = 0; c < 5; c++)
                        {
                            gridModel.Data.Columns.Add(new DataColumn($"COLUMN_{c}"));
                        }

                        for (int r = 0; r < 5; r++)
                        {
                            DataRow row = gridModel.Data.NewRow();
                            for (int c = 0; c < 5; c++)
                            {
                                row[$"COLUMN_{c}"] = "xxxxx";
                            }
                            gridModel.Data.Rows.Add(row);
                        }

                        PluginHelper.InvokeMethod(typeof(CustomGridPlugin), methodName, gridModel);
                        foreach (DataRow row in gridModel.Data.Rows)
                        {
                            foreach (DataColumn column in gridModel.Data.Columns)
                            {
                                Assert.That(row[column], Is.EqualTo("TRANSFORMED"));
                            }
                        }

                        break;
                }
            }
        }

        [Test]
        public void CustomFormPluginNotImplementedTest()
        {
            object? returnValue = null;
            foreach (string methodName in ICustomFormPluginMethodNames)
            {
                returnValue = PluginHelper.InvokeMethod(typeof(CustomFormPluginNotImplemented), methodName, new FormModel(), null);
                Assert.IsNull(returnValue);
            }
        }

        [Test]
        public void CustomFormPluginExceptionTest()
        {
            foreach (string methodName in ICustomFormPluginMethodNames)
            {
                FormModel formModel = new FormModel();
                PluginHelper.InvokeMethod(typeof(CustomFormPluginException), methodName, formModel);
                Assert.IsTrue(formModel.Message == CustomGridPluginBase.TestExceptionMessage);
            }
        }

        [Test]
        public void CustomFormPluginTest()
        {
            foreach (string methodName in ICustomFormPluginMethodNames)
            {
                FormModel formModel = new FormModel();
                switch (methodName)
                {
                    case nameof(ICustomFormPlugin.Initialisation):
                        PluginHelper.InvokeMethod(typeof(CustomFormPlugin), methodName, formModel);
                        Assert.IsTrue(formModel.Columns.Count() == 1);
                        Assert.IsTrue(formModel.Columns.First().Expression == "TEST");
                        break;
                    case nameof(ICustomFormPlugin.ValidateUpdate):
                    case nameof(ICustomFormPlugin.ValidateInsert):
                    case nameof(ICustomFormPlugin.ValidateDelete):
                        bool? returnValue = (bool?)PluginHelper.InvokeMethod(typeof(CustomFormPlugin), methodName, formModel);
                        Assert.IsTrue(returnValue.HasValue && returnValue.Value == false);
                        break;
                }
            }
        }
    }
}
