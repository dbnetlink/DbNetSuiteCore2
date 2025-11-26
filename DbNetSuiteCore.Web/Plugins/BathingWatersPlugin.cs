using DbNetSuiteCore.Plugins.Interfaces;
using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Web.Plugins
{
    public class BathingWatersPlugin : ICustomGridPlugin
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
            foreach (DataRow dataRow in gridModel.Data.Rows)
            {
                if (dataRow.ItemArray[3] == DBNull.Value)
                {
                    dataRow.Delete();
                }
            }

            gridModel.Data.AcceptChanges();

            for (int c = 0; c < gridModel.Columns.Count(); c++)
            {
                GridColumn gridColumn = gridModel.Columns.ToList()[c];
                gridColumn.Label = gridModel.Data.Rows[0][c]?.ToString() ?? string.Empty;

                if (gridColumn.Label.Contains("%"))
                {
                    gridColumn.DataType = typeof(double);
                }
            }

            gridModel.Data.Rows.RemoveAt(0);
            gridModel.Data.Columns.RemoveAt(0);
        }
    }

}