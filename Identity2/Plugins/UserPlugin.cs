using DbNetSuiteCore.Plugins.Interfaces;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.Identity.Plugins
{
    public class UserPlugin : ICustomFormPlugin
    {
        public bool ValidateUpdate(FormModel formModel)
        {
            if (formModel.FormValues.ContainsKey("Email"))
            {
                if (ValidationHelper.IsValidEmail(formModel.FormValues["Email"]) == false)
                {
                    formModel.Message = "Format of email address is not valid";
                    formModel.Columns.First(c => c.Name == "Email").InError = true;
                    return false;
                }
            }
            return true;
        }

        public bool ValidateInsert(FormModel formModel)
        {
           return true;
        }

        public bool ValidateDelete(FormModel formModel)
        {
            formModel.Message = "Cannot delete User";
            return false;
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

