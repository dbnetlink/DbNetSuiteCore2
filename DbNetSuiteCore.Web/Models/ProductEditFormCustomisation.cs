using DbNetSuiteCore.Plugins.Interfaces;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.Web.Models
{
    public class ProductEditFormCustomisation : ICustomFormPlugin
    {
        public bool ValidateUpdate(FormModel formModel)
        {
            var reorderLevel = Convert.ToInt32(formModel.FormValue("reorderlevel"));
            var discontinued = Boolean.Parse(formModel.FormValue("discontinued")?.ToString() ?? string.Empty);

            if (discontinued && reorderLevel > 0)
            {
                formModel.Message = "Re-order level must be zero for discontinued products";
                return false;
            }

            return true;
        }

        public bool ValidateInsert(FormModel formModel)
        {
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

