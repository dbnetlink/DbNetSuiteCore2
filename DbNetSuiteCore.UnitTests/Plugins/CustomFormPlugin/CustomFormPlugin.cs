using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.UnitTests.Plugins.CustomFormPlugin
{
    public class CustomFormPlugin : CustomFormPluginBase    
    {
        public override void Initialisation(FormModel formModel)
        {
            formModel.Columns = new List<FormColumn>() { new FormColumn("TEST") };
        }
        public override bool ValidateUpdate(FormModel formModel)
        {
            return false;
        }
        public override bool ValidateInsert(FormModel formModel)
        {
            return false;
        }
        public override bool ValidateDelete(FormModel formModel)
        {
            return false;
        }
        public override void CustomCommit(FormModel formModel)
        {
        }
    }
}
