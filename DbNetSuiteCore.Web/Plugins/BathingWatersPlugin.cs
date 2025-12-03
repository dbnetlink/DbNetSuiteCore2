using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using DbNetSuiteCore.Extensions;
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
            gridModel.Columns.Last().Visible = false;
            while (dataTable.Rows.Count > 0)
            {
                DataRow dataRow = dataTable.Rows.Cast<DataRow>().Last();
                if (dataRow[1] != DBNull.Value && string.IsNullOrEmpty(dataRow.ItemArray[1]?.ToString()) == false)
                {
                    break;
                }
                dataTable.Rows.Remove(dataRow);
            }

            switch (gridModel.SheetName)
            {
                case "Class_Summary":
                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        foreach (DataColumn dataColumn in dataTable.Columns)
                        {
                            var strValue = dataRow[dataColumn]?.ToString() ?? string.Empty;
                            if (strValue.EndsWith("%"))
                            {
                                dataRow[dataColumn] = (Double.Parse(strValue.Replace("%", string.Empty)) / 100).ToString();
                            }
                        }
                    }
                    foreach (DataColumn dataColumn in dataTable.Columns.Cast<DataColumn>().Skip(2).ToList())
                    {
                        dataTable.UpdateColumnDataType(dataColumn, typeof(double));
                    }
                    break;
                default:
                    break;
            }
        }

        private void DeleteUpToHeadings(string heading, DataTable dataTable)
        {
            foreach (DataRow dataRow in dataTable.Rows)
            {
                if (dataRow.ItemArray[1]?.ToString() == heading)
                {
                    break;
                }
                dataRow.Delete();
            }

            dataTable.AcceptChanges();
        }

        private void ConfigureClassSummary(GridModel gridModel, DataTable dataTable)
        {
            DeleteUpToHeadings("EA Areas", dataTable);
            for (int c = 0; c < gridModel.Columns.Count(); c++)
            {
                GridColumn gridColumn = gridModel.Columns.ToList()[c];
                gridColumn.Label = dataTable.Rows[0][c]?.ToString() ?? string.Empty;

                if (c > 1)
                {
                    gridColumn.UserDataType = typeof(double).Name;
                }
                if (gridColumn.Label.Contains("%"))
                {
                    gridColumn.Format = "p";
                }

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
            DeleteUpToHeadings("Bathing Water Reference", dataTable);

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
            DeleteUpToHeadings("EA Area", dataTable);
            for (int c = 0; c < gridModel.Columns.Count(); c++)
            {
                GridColumn gridColumn = gridModel.Columns.ToList()[c];
                gridColumn.Label = dataTable.Rows[0][c]?.ToString() ?? string.Empty;
                gridColumn.Filter = DbNetSuiteCore.Enums.FilterType.Distinct;
            }
        }
    }
}