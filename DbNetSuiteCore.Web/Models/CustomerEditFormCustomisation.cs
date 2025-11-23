using DbNetSuiteCore.Plugins.Interfaces;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DocumentFormat.OpenXml.Office2010.Excel;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace DbNetSuiteCore.Web.Models
{
    public class CustomerEditFormCustomisation : ICustomFormPlugin
    {
        public bool ValidateUpdate(FormModel formModel)
        {
            return true;
        }

        public bool ValidateInsert(FormModel formModel)
        {
           return true;
        }

        public bool ValidateDelete(FormModel formModel)
        {
            var httpContext2 = formModel.HttpContext;
            var configuration1 = formModel.Configuration;

            var dataTable = DbHelper.GetRecord(formModel);

            if (dataTable.Rows[0]["CompanyName"].ToString() != "DbNetLink Limited")
            {
                formModel.Message = "Company Name must be 'DbNetLink Limited' to be deleted";
                return false;
            }
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

