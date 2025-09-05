using DbNetSuiteCore.CustomisationHelpers.Interfaces;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DocumentFormat.OpenXml.Office2010.Excel;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace DbNetSuiteCore.Web.Models
{
    public class AspNetUserRolesCustomisation : ICustomForm
    {
        public bool ValidateFormUpdate(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            return true;
        }

        public bool ValidateFormInsert(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            formModel.FormValues["userid"] = TextHelper.DeobfuscateString(formModel.FormValues["userid"]);
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

