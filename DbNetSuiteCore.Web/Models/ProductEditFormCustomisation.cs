using DbNetSuiteCore.Plugins.Interfaces;
using DbNetSuiteCore.Models;
using DocumentFormat.OpenXml.Office2010.Excel;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace DbNetSuiteCore.Web.Models
{
    public class ProductEditFormCustomisation : ICustomFormPlugin
    {
        public bool ValidateUpdate(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
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

        public bool ValidateInsert(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
           return true;
        }

        public bool ValidateDelete(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            return true;
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

