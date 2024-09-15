using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Extensions
{
    public static class DataTableExtensions
    {
        public static void ConvertLookupColumn(this DataTable dataTable, DataColumn? currentColumn, GridColumnModel gridColumnModel, GridModel gridModel)
        {
            if (currentColumn == null)
            {
                return;
            }

            dataTable.PrimaryKey = null;

            if (currentColumn.DataType != typeof(string))
            {
                using (DataColumn newColumn = new DataColumn($"{currentColumn.ColumnName}_lookup_", typeof(string)))
                {
                    int ordinal = dataTable.Columns[currentColumn.ColumnName].Ordinal;
                    dataTable.Columns.Add(newColumn);
                    newColumn.SetOrdinal(ordinal);

                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        dataRow[newColumn] = gridColumnModel.GetLookupValue(dataRow[currentColumn]);
                    }
                    dataTable.Columns.Remove(currentColumn);
                    newColumn.ColumnName = currentColumn.ColumnName;
                }
            }
            else
            {
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    dataRow[currentColumn] = gridColumnModel.GetLookupValue(dataRow[currentColumn]);
                }
            }
        }
    }
}
