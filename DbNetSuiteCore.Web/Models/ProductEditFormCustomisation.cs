using DbNetSuiteCore.CustomisationHelpers.Interfaces;
using DbNetSuiteCore.Models;
using DocumentFormat.OpenXml.Office2010.Excel;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace DbNetSuiteCore.Web.Models
{
    public class ProductEditFormCustomisation : ICustomForm
    {
        public bool ValidateFormUpdate(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            var reorderLevel = Convert.ToInt32(formModel.FormValue("reorderlevel"));
            var discontinued = Boolean.Parse(formModel.FormValue("discontinued").ToString());

            if (discontinued && reorderLevel > 0)
            {
                formModel.Message = "Re-order level must be zero for discontinued products";
                return false;
            }

            return true;
        }

        public bool ValidateFormInsert(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
           return true;
        }

        public bool ValidateFormDelete(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            return true;
        }
        public void FormInitialisation(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
        }
    }
}

