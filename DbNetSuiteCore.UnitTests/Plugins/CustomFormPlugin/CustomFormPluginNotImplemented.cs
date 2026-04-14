using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.UnitTests.Plugins.CustomFormPlugin
{
    public class CustomFormPluginNotImplemented : CustomFormPluginBase    
    {
        public override void Initialisation(FormModel formModel)
        {
            throw new NotImplementedException();
        }
        public override bool ValidateUpdate(FormModel formModel)
        {
            throw new NotImplementedException();
        }
        public override bool ValidateInsert(FormModel formModel)
        {
            throw new NotImplementedException();
        }
        public override bool ValidateDelete(FormModel formModel)
        {
            throw new NotImplementedException();
        }
        public override void CustomCommit(FormModel formModel)
        {
            throw new NotImplementedException();
        }
    }
}
