using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
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
            DataTable dataTable = gridModel.Data;

            foreach (DataRow dataRow in dataTable.Rows)
            {
                if (dataRow.ItemArray[1] == DBNull.Value)
                {
                    dataRow.Delete();
                    break;
                }
                dataRow.Delete();
            }

            dataTable.AcceptChanges();

            switch (gridModel.SheetName)
            {
                case "Class_Results":
                    ConfigureClassResults(gridModel, dataTable);
                    break;
                case "Class_Comms":
                    ConfigureClassComms(gridModel, dataTable);
                    break;
                default:
                    ConfigureClassSummary(gridModel, dataTable);
                    break;
            }

            dataTable.Rows.RemoveAt(0);
            gridModel.Columns.First().Visible = false;

            while (dataTable.Rows.Count > 0)
            {
                DataRow dataRow = dataTable.Rows.Cast<DataRow>().Last();
                if (dataRow[1] != DBNull.Value)
                {
                    break;
                }
                dataTable.Rows.Remove(dataRow);
            }
        }

        private void ConfigureClassSummary(GridModel gridModel, DataTable dataTable)
        {
            for (int c = 0; c < gridModel.Columns.Count(); c++)
            {
                GridColumn gridColumn = gridModel.Columns.ToList()[c];
                gridColumn.Label = dataTable.Rows[0][c]?.ToString() ?? string.Empty;

                if (c > 1)
                {
                    gridColumn.DataType = typeof(double);
                }
                if (gridColumn.Label.Contains("%"))
                {
                    gridColumn.Format = "p";
                }
                /*
                if (gridColumn.Label == "Total number of BWs classified")
                {
                    gridColumn.Label = "Total";
                }
                else if (gridColumn.Label.EndsWith("% Compliant Waters"))
                {
                    gridColumn.Label = "Compliant Waters %";
                }
                else if (gridColumn.Label.EndsWith("Compliant Waters"))
                {
                    gridColumn.Label = "Compliant Waters";
                }
                */

                if (gridColumn.Label.StartsWith("Excellent"))
                {
                    gridColumn.Style = "background-color:skyblue";
                }
                if (gridColumn.Label.StartsWith("Good"))
                {
                    gridColumn.Style = "background-color:palegreen";
                }
                if (gridColumn.Label.StartsWith("Sufficient"))
                {
                    gridColumn.Style = "background-color:gold";
                }
                if (gridColumn.Label.StartsWith("Poor"))
                {
                    gridColumn.Style = "background-color:salmon";
                }
            }

            foreach (int i in Enumerable.Range(0, 3))
            {
                dataTable.Rows.RemoveAt(dataTable.Rows.Count - 1);
            }

        }

        private void ConfigureClassResults(GridModel gridModel, DataTable dataTable)
        {
            for (int c = 0; c < gridModel.Columns.Count(); c++)
            {
                GridColumn gridColumn = gridModel.Columns.ToList()[c];
                gridColumn.Label = dataTable.Rows[0][c]?.ToString() ?? string.Empty;

                if (gridColumn.Label == "2025 Classification")
                {
                    gridColumn.Filter = DbNetSuiteCore.Enums.FilterType.Distinct;
                }
            }
        }

        private void ConfigureClassComms(GridModel gridModel, DataTable dataTable)
        {
            for (int c = 0; c < gridModel.Columns.Count(); c++)
            {
                GridColumn gridColumn = gridModel.Columns.ToList()[c];
                gridColumn.Label = dataTable.Rows[0][c]?.ToString() ?? string.Empty;
                gridColumn.Filter = DbNetSuiteCore.Enums.FilterType.Distinct;
            }
        }
    }
}