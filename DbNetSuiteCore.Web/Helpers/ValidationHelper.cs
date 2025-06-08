using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using DocumentFormat.OpenXml.Spreadsheet;

namespace DbNetSuiteCore.Web.Helpers
{
    public static class ValidationHelper
    {
        public const string ProductEditGrid = "ProductEditGrid";
        public const string ProductEditForm = "ProductEditForm";
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

        public static bool ValidateFormInsert(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            switch (formModel.TableName)
            {
                case "AspNetRoles":
                    QueryCommandConfig query = new QueryCommandConfig(formModel.DataSourceType) { Sql = "select id from AspNetRoles where name = @name" };
                    query.Params["name"] = formModel.FormValues["Name"];

                    if (DbHelper.RecordExists(query, formModel.ConnectionAlias, formModel.DataSourceType, configuration))
                    {
                        formModel.Message = "A role already exists with this name";
                        return false;
                    }
                    break;
            }
            return true;
        }

        public static bool ValidateFormDelete(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            switch (formModel.TableName)
            {
                case "AspNetUsers":
                    formModel.Message = "Cannot delete User";
                    return false;
            }
            return true;
        }

        public static bool ValidateGridUpdate(GridModel gridModel, HttpContext httpContext, IConfiguration configuration)
        {
            switch (gridModel.Name)
            {
                case ProductEditGrid:
                    foreach (var row in gridModel.ModifiedRows().Keys)
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

    }
}