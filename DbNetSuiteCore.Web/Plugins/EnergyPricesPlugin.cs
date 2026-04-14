using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using System.Data;

namespace DbNetSuiteCore.Web.Plugins
{
    public class EnergyPricesPlugin : ICustomGridPlugin
    {
        public bool ValidateUpdate(GridModel gridModel)
        {
            throw new NotImplementedException();
        }
        public void Initialisation(GridModel gridModel)
        {
        }
        public void TransformDataTable(GridModel gridModel)
        {
            DataTable dataTable = gridModel.Data;

            foreach (DataRow dataRow in dataTable.Rows)
            {
                if (dataRow.ItemArray[2] == DBNull.Value || (dataRow.ItemArray[0]?.ToString() ?? string.Empty).ToLower().Contains("rank"))
                {
                    dataRow.Delete();
                }
            }

            dataTable.AcceptChanges();
        }

    }
}