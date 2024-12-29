using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Extensions
{
    public static class DataTableExtensions
    {
        public static void ConvertLookupColumn(this DataTable dataTable, DataColumn? currentColumn, ColumnModel gridColumnModel, ComponentModel gridModel)
        {
            if (currentColumn == null)
            {
                return;
            }

            dataTable.PrimaryKey = null;
            currentColumn.ReadOnly = false;

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

        public static void FilterAndSort(this DataTable dataTable, GridModel gridModel)
        {
            var rows = dataTable.Select(AddFilter(gridModel), AddOrder(gridModel));

            if (rows.Any())
            {
                gridModel.Data = rows.CopyToDataTable();
            }
            else
            {
                gridModel.Data = new DataTable();
            }
        }

        public static void FilterAndSort(this DataTable dataTable, SelectModel selectModel)
        {
            var rows = dataTable.Select(AddFilter(selectModel), AddOrder(selectModel));

            if (rows.Any())
            {
                selectModel.Data = rows.CopyToDataTable();
            }
            else
            {
                selectModel.Data = new DataTable();
            }
        }

        public static void FilterWithPrimaryKey(this DataTable dataTable, ComponentModel componentModel)
        {
            var rows = dataTable.Select(AddPrimaryKeyFilter(componentModel));

            if (rows.Any())
            {
                componentModel.Data = rows.CopyToDataTable();
            }
            else
            {
                componentModel.Data = new DataTable();
            }
        }

        private static string AddFilter(GridModel gridModel)
        {
            string filter = string.Empty;
            List<string> filterParts = new List<string>();
            if (string.IsNullOrEmpty(gridModel.SearchInput) == false)
            {
                List<string> searchFilterPart = new List<string>();

                foreach (var columnName in gridModel.SearchableColumns.Select(c => c.Name).ToList())
                {
                    searchFilterPart.Add($"{TextHelper.DelimitColumn(columnName, gridModel.DataSourceType)} like '%{gridModel.SearchInput}%'");
                }

                if (searchFilterPart.Any())
                {
                    filterParts.Add($"({string.Join(" or ", searchFilterPart)})");
                }
            }

            if (gridModel.FilterColumns.Any())
            {
                List<string> columnFilterPart = new List<string>();
                for (var i = 0; i < gridModel.ColumnFilter.Count; i++)
                {
                    if (string.IsNullOrEmpty(gridModel.ColumnFilter[i]))
                    {
                        continue;
                    }

                    var column = gridModel.Columns.Where(c => c.Filter != Enums.FilterType.None).Skip(i).First();

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
                        column.FilterError = ResourceHelper.GetResourceString(ResourceNames.DataFormatError);
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

        private static string AddFilter(SelectModel selectModel)
        {
            string filter = string.Empty;
            List<string> filterParts = new List<string>();
            if (string.IsNullOrEmpty(selectModel.SearchInput) == false)
            {
                List<string> searchFilterPart = new List<string>();

                foreach (var columnName in selectModel.SearchableColumns.Select(c => c.Name).ToList())
                {
                    searchFilterPart.Add($"{TextHelper.DelimitColumn(columnName, selectModel.DataSourceType)} like '%{selectModel.SearchInput}%'");
                }

                if (searchFilterPart.Any())
                {
                    filterParts.Add($"({string.Join(" or ", searchFilterPart)})");
                }
            }

            if (selectModel.IsLinked)
            {
                if (!string.IsNullOrEmpty(selectModel.ParentKey))
                {
                    var foreignKeyColumn = selectModel.GetColumns().FirstOrDefault(c => c.ForeignKey);
                    if (foreignKeyColumn != null)
                    {
                        filterParts.Add($"({TextHelper.DelimitColumn(foreignKeyColumn.Name, selectModel.DataSourceType)} = {Quoted(foreignKeyColumn)}{selectModel.ParentKey}{Quoted(foreignKeyColumn)})");
                    }
                }
                else
                {
                    filterParts.Add("(1=2)");
                }
            }

            if (!string.IsNullOrEmpty(selectModel.FixedFilter))
            {
                filterParts.Add($"({selectModel.FixedFilter})");
            }

            return String.Join(" and ", filterParts);
        }

        private static string AddPrimaryKeyFilter(ComponentModel componentModel)
        {
            var primaryKeyColumn = componentModel.GetColumns().FirstOrDefault(c => c.PrimaryKey);
            return $"({primaryKeyColumn.Name} = {Quoted(primaryKeyColumn)}{componentModel.ParentKey}{Quoted(primaryKeyColumn)})";
        }

        public static string AddOrder(GridModel gridModel)
        {
            if (string.IsNullOrEmpty(gridModel.SortColumnName))
            {
                return string.Empty;
            }

            return $"{TextHelper.DelimitColumn(gridModel.SortColumnName, gridModel.DataSourceType)} {gridModel.SortSequence}";
        }

        public static string AddOrder(SelectModel selectModel)
        {
            string optionGroupSortColumnName = string.Empty;

            if (selectModel.IsGrouped)
            {
                optionGroupSortColumnName = $"{TextHelper.DelimitColumn(selectModel.OptionGroupColumn.ColumnName, selectModel.DataSourceType)},";
            }

            return $"{optionGroupSortColumnName}{TextHelper.DelimitColumn(selectModel.SortColumnName, selectModel.DataSourceType)}";
        }

        private static string Quoted(ColumnModel column)
        {
            return (new string[] { nameof(String), nameof(DateTime) }).Contains(column.DataTypeName) ? "'" : string.Empty;
        }
    }
}
