using DbNetSuiteCore.CustomisationHelpers.Interfaces;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DocumentFormat.OpenXml.Office2010.Excel;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace DbNetSuiteCore.Web.Models
{
    public class AspNetUsersCustomisation : ICustomForm
    {
        public bool ValidateFormUpdate(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
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

        public bool ValidateFormInsert(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
           return true;
        }

        public bool ValidateFormDelete(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            formModel.Message = "Cannot delete User";
            return false;
        }
        public void FormInitialisation(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
        }
    }
}

