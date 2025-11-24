using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using System.Collections;

namespace DbNetSuiteCore.UnitTests.Plugins.CustomGridPlugin
{
    public abstract class CustomGridPluginBase : ICustomGridPlugin
    {
        public static string TestExceptionMessage = "This is a test exception message.";
        public abstract void Initialisation(GridModel gridModel);
        public abstract bool ValidateUpdate(GridModel gridModel);
        public abstract void TransformDataTable(GridModel gridModel);
    }
}
