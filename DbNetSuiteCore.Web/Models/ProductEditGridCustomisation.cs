using DbNetSuiteCore.Plugins.Interfaces;
using DbNetSuiteCore.Models;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace DbNetSuiteCore.Web.Models
{
    public class ProductEditGridCustomisation : ICustomGridPlugin
    {
        public bool ValidateUpdate(GridModel gridModel, HttpContext httpContext, IConfiguration configuration)
        {
            foreach (var row in gridModel.ModifiedRows.Keys)
            {
                var reorderLevel = Convert.ToInt32(gridModel.FormValues["reorderlevel"][row]);
                var discontinued = Convert.ToBoolean(gridModel.FormValues["discontinued"][row]);

                if (discontinued && reorderLevel > 0)
                {
                    gridModel.ModifiedRows[row].InError = true;
                }
            }

            if (gridModel.ModifiedRows.Any(r => r.Value.InError))
            {
                gridModel.Message = "Re-order level must be zero for discontinued products";
                return false;
            }

            return true;
        }
        public void Initialisation(GridModel gridModel, HttpContext httpContext, IConfiguration configuration)
        {
        }
    }
}

