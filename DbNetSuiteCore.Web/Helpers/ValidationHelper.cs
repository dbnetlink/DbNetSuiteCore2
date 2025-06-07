using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using DocumentFormat.OpenXml.Drawing.Charts;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DbNetSuiteCore.Web.Helpers
{
    public static class ValidationHelper
    {
        public static bool ValidateFormUpdate(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            switch (formModel.Name)
            {
                case "AspNetRoles":
                    if (formModel.FormValues.ContainsKey("Name"))
                    {
                        QueryCommandConfig query = new QueryCommandConfig(formModel.DataSourceType) { Sql = "select id from AspNetRoles where name = @name and id != @id" };
                        query.Params["name"] = formModel.FormValues["Name"];
                        query.Params["id"] = formModel.RecordId;

                        if (DbHelper.RecordExists(query, formModel.ConnectionAlias, formModel.DataSourceType, configuration))
                        {
                            formModel.Message = "A role already exists with this name";
                            return false;
                        }
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
                case "ProductEditGrid":
                    foreach (var row in gridModel.ModifiedRows().Keys)
                    {
                        var modifiedRow = gridModel.ModifiedRows()[row];

                        var reorderLevel = Convert.ToInt32(gridModel.FormValues["ReorderLevel"][row]);
                        var discontinued = Convert.ToBoolean(gridModel.FormValues["Discontinued"][row]);

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