using DbNetSuiteCore.Plugins.Interfaces;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DocumentFormat.OpenXml.Office2010.Excel;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace DbNetSuiteCore.Identity.Plugins
{
    public class UserPlugin : ICustomFormPlugin
    {
        public bool ValidateUpdate(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
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

        public bool ValidateInsert(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
           return true;
        }

        public bool ValidateDelete(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            formModel.Message = "Cannot delete User";
            return false;
        }
        public void Initialisation(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
        }

        public void CustomCommit(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            throw new NotImplementedException();
        }
    }
}

