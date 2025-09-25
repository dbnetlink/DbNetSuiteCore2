using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Middleware;
using System;

namespace DbNetSuiteCore.Web.Helpers
{
    public static class OptionsHelper
    {
        public const string ProductEditGrid = "ProductEditGrid";
        public const string ProductEditForm = "ProductEditForm";
        public const string CustomerEditForm = "CustomerEditForm";
        public const string LeakReportGrid = "LeakReportGrid";

      
        public static bool ValidateFormUpdate(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            switch (formModel.Name)
            {
                case ProductEditForm:
                    var reorderLevel = Convert.ToInt32(formModel.FormValue("reorderlevel"));
                    var discontinued = Boolean.Parse(formModel.FormValue("discontinued").ToString());

                    if (discontinued && reorderLevel > 0)
                    {
                        formModel.Message = "Re-order level must be zero for discontinued products";
                        return false;
                    }
                    break;
            }
            return true;
        }

        public static bool ValidateFormDelete(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            switch (formModel.Name)
            {
                case CustomerEditForm:
                    var dataTable = DbHelper.GetRecord(formModel, httpContext);

                    if (dataTable.Rows[0]["CompanyName"].ToString() != "DbNetLink Limited")
                    {
                        formModel.Message = "Company Name must be 'DbNetLink Limited' to be deleted";
                        return false;
                    }
                    break;
                    
            }
            return true;
        }

        public static bool ValidateGridUpdate(GridModel gridModel, HttpContext httpContext, IConfiguration configuration)
        {
            switch (gridModel.Name)
            {
                case ProductEditGrid:
                    foreach (var row in gridModel.ModifiedRows.Keys)
                    {
                        var reorderLevel = Convert.ToInt32(gridModel.FormValues["reorderlevel"][row]);
                        var discontinued = Convert.ToBoolean(gridModel.FormValues["discontinued"][row]);

                        if (discontinued && reorderLevel > 0)
                        {
                            gridModel.Message = "Re-order level must be zero for discontinued products";
                            return false;
                        }
                    }

                    break;
            }
            return true;
        }

        public static bool GridInitialisation(GridModel gridModel, HttpContext httpContext, IConfiguration configuration)
        {
            switch (gridModel.Name)
            {
                case LeakReportGrid:
                    gridModel.Columns.First(c => c.Name == "ASSET_ID").PrimaryKey = false;
                    break;
            }
            return true;
        }

    }
}