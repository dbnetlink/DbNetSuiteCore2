using DbNetSuiteCore.CustomisationHelpers.Interfaces;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DocumentFormat.OpenXml.Office2010.Excel;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace DbNetSuiteCore.Web.Models
{
    public class CustomerEditFormCustomisation : ICustomFormPlugin
    {
        public bool ValidateUpdate(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            return true;
        }

        public bool ValidateInsert(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
           return true;
        }

        public bool ValidateDelete(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            var dataTable = DbHelper.GetRecord(formModel, httpContext);

            if (dataTable.Rows[0]["CompanyName"].ToString() != "DbNetLink Limited")
            {
                formModel.Message = "Company Name must be 'DbNetLink Limited' to be deleted";
                return false;
            }
            return true;
        }
        public void Initialisation(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
        }
    }
}

