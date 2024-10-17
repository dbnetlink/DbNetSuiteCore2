using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Extensions
{
    public static class DataTableExtensions
    {
        public static void ConvertLookupColumn(this DataTable dataTable, DataColumn? currentColumn, GridColumn gridColumnModel, GridModel gridModel)
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
                currentColumn.MaxLength = -1;
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    dataRow[currentColumn] = gridColumnModel.GetLookupValue(dataRow[currentColumn]);
                }
            }
        }

        public static void FilterAndSort(this DataTable dataTable, GridModel gridModel, bool addFilter = true)
        {
            var rows = dataTable.Select(addFilter ? AddFilter(gridModel) : string.Empty, AddOrder(gridModel));

            if (rows.Any())
            {
                gridModel.Data = rows.CopyToDataTable();
            }
            else
            {
                gridModel.Data = new DataTable();
            }
        }

        public static void FilterWithPrimaryKey(this DataTable dataTable, GridModel gridModel)
        {
            var rows = dataTable.Select(AddPrimaryKeyFilter(gridModel));

            if (rows.Any())
            {
                gridModel.Data = rows.CopyToDataTable();
            }
            else
            {
                gridModel.Data = new DataTable();
            }
        }

        private static string AddFilter(GridModel gridModel)
        {
            string filter = string.Empty;
            List<string> filterParts = new List<string>();
            if (string.IsNullOrEmpty(gridModel.SearchInput) == false)
            {
                List<string> searchFilterPart = new List<string>();

                foreach (var columnName in gridModel.Columns.Where(c => c.Searchable).Select(c => c.Name).ToList())
                {
                    searchFilterPart.Add($"{TextHelper.DelimitColumn(columnName, gridModel.DataSourceType)} like '%{gridModel.SearchInput}%'");
                }

                if (searchFilterPart.Any())
                {
                    filterParts.Add($"({string.Join(" or ", searchFilterPart)})");
                }
            }

            if (gridModel.Columns.Any(c => c.Filter))
            {
                List<string> columnFilterPart = new List<string>();
                for (var i = 0; i < gridModel.ColumnFilter.Count; i++)
                {
                    if (string.IsNullOrEmpty(gridModel.ColumnFilter[i]))
                    {
                        continue;
                    }

                    var column = gridModel.Columns.Where(c => c.Filter).Skip(i).First();

                    var columnFilter = GridModelExtensions.ParseFilterColumnValue(gridModel.ColumnFilter[i], column);

                    if (columnFilter != null)
                    {
                        if (string.IsNullOrEmpty(columnFilter.Value.ToString()) == false)
                        {
                            columnFilterPart.Add($"{TextHelper.DelimitColumn(column.Name, gridModel.DataSourceType)} {columnFilter.Value.Key} {Quoted(column)}{columnFilter.Value.Value}{Quoted(column)}");
                        }
                        else
                        {
                            column.FilterError = ResourceHelper.GetResourceString(ResourceNames.ColumnFilterNoData);
                        }
                    }
                    else
                    {
                        column.FilterError = ResourceHelper.GetResourceString(ResourceNames.ColumnFilterDataError);
                    }
                }

                if (columnFilterPart.Any())
                {
                    filterParts.Add($"({string.Join(" and ", columnFilterPart)})");
                }
            }

            if (gridModel.IsNested || gridModel.IsLinked)
            {
                if (!string.IsNullOrEmpty(gridModel.ParentKey))
                {
                    var foreignKeyColumn = gridModel.Columns.FirstOrDefault(c => c.ForeignKey);
                    if (foreignKeyColumn != null)
                    {
                        filterParts.Add($"({TextHelper.DelimitColumn(foreignKeyColumn.Name, gridModel.DataSourceType)} = {Quoted(foreignKeyColumn)}{gridModel.ParentKey}{Quoted(foreignKeyColumn)})");
                    }
                }
                else
                {
                    filterParts.Add("(1=2)");
                }
            }

            if (!string.IsNullOrEmpty(gridModel.FixedFilter))
            {
                filterParts.Add($"({gridModel.FixedFilter})");
            }

            return String.Join(" and ", filterParts);
        }

        private static string AddPrimaryKeyFilter(GridModel gridModel)
        {
            var primaryKeyColumn = gridModel.Columns.FirstOrDefault(c => c.PrimaryKey);
            return $"({primaryKeyColumn.Name} = {Quoted(primaryKeyColumn)}{gridModel.ParentKey}{Quoted(primaryKeyColumn)})";
        }

        private static string AddOrder(GridModel gridModel)
        {
            if (string.IsNullOrEmpty(gridModel.SortColumnName))
            {
                return string.Empty;
            }

            return $"{TextHelper.DelimitColumn(gridModel.SortColumnName, gridModel.DataSourceType)} {gridModel.SortSequence}";
        }

        private static string Quoted(GridColumn column)
        {
            return (new string[] { nameof(String), nameof(DateTime) }).Contains(column.DataTypeName) ? "'" : string.Empty;
        }
    }
}
