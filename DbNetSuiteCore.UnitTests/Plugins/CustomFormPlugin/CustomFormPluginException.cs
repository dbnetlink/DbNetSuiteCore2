using DbNetSuiteCore.Models;
using DbNetSuiteCore.UnitTests.Plugins.CustomGridPlugin;


namespace DbNetSuiteCore.UnitTests.Plugins.CustomFormPlugin
{
    public class CustomFormPluginException : CustomFormPluginBase    
    {
        public override void Initialisation(FormModel formModel)
        {
            throw new Exception(CustomGridPluginBase.TestExceptionMessage);
        }
        public override bool ValidateUpdate(FormModel formModel)
        {
            throw new Exception(CustomGridPluginBase.TestExceptionMessage);
        }
        public override bool ValidateInsert(FormModel formModel)
        {
            throw new Exception(CustomGridPluginBase.TestExceptionMessage);
        }
        public override bool ValidateDelete(FormModel formModel)
        {
            throw new Exception(CustomGridPluginBase.TestExceptionMessage);
        }
        public override void CustomCommit(FormModel formModel)
        {
            throw new Exception(CustomGridPluginBase.TestExceptionMessage);
        }
    }
}
