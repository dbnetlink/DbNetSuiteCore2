using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using System.Collections;

namespace DbNetSuiteCore.UnitTests.Plugins.CustomFormPlugin
{
    public abstract class CustomFormPluginBase : ICustomFormPlugin
    {
        public static string TestExceptionMessage = "This is a test exception message.";
        public abstract void Initialisation(FormModel formModel);
        public abstract bool ValidateUpdate(FormModel formModel);
        public abstract bool ValidateInsert(FormModel formModel);
        public abstract bool ValidateDelete(FormModel formModel);
        public abstract void CustomCommit(FormModel formModel);
    }
}
