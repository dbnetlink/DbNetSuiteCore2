using DbNetSuiteCore.Plugins.Interfaces;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.Identity.Plugins
{
    public class UserRolePlugin: ICustomFormPlugin
    {
        public bool ValidateUpdate(FormModel formModel)
        {
            return true;
        }

        public bool ValidateInsert(FormModel formModel)
        {
            formModel.FormValues["userid"] = TextHelper.DeobfuscateString(formModel.FormValues["userid"]);
            return true;
        }

        public bool ValidateDelete(FormModel formModel)
        {
            return true;
        }
        public void Initialisation(FormModel formModel)
        {
        }
        public void CustomCommit(FormModel formModel)
        {
            throw new NotImplementedException();
        }
    }
}

