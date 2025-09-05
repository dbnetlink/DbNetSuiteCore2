using DbNetSuiteCore.CustomisationHelpers.Interfaces;
using DbNetSuiteCore.Models;
using DocumentFormat.OpenXml.Office2010.Excel;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace DbNetSuiteCore.Web.Models
{
    public class ProductEditGridCustomisation : ICustomGrid
    {
        public bool ValidateGridUpdate(GridModel gridModel, HttpContext httpContext, IConfiguration configuration)
        {
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

            return true;
        }
        public void GridInitialisation(GridModel gridModel, HttpContext httpContext, IConfiguration configuration)
        {
        }
    }
}

