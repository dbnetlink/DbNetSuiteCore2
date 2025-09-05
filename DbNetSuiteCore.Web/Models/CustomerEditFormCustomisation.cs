using DbNetSuiteCore.CustomisationHelpers.Interfaces;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DocumentFormat.OpenXml.Office2010.Excel;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace DbNetSuiteCore.Web.Models
{
    public class CustomerEditFormCustomisation : ICustomForm
    {
        public bool ValidateFormUpdate(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            return true;
        }

        public bool ValidateFormInsert(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
           return true;
        }

        public bool ValidateFormDelete(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            var dataTable = DbHelper.GetRecord(formModel, httpContext);

            if (dataTable.Rows[0]["CompanyName"].ToString() != "DbNetLink Limited")
            {
                formModel.Message = "Company Name must be 'DbNetLink Limited' to be deleted";
                return false;
            }
            return true;
        }
        public void FormInitialisation(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
        }
    }
}

