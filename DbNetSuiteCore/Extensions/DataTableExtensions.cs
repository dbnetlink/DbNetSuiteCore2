using DbNetSuiteCore.Attributes;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using System.ComponentModel.DataAnnotations;
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
                    if (gridColumnModel.PrimaryKey)
                    {
                        var columnName = currentColumn.ColumnName;
                        currentColumn.ColumnName = $"{columnName}_value";
                        newColumn.ColumnName = columnName;
                    }
                    else
                    {
                        dataTable.Columns.Remove(currentColumn);
                        newColumn.ColumnName = currentColumn.ColumnName;
                    }
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

        public static void UpdateColumnDataType(this DataTable dt, string colName, Type dataType)
        {
            DataColumn? dataColumn = dt.Columns.Cast<DataColumn>().FirstOrDefault(c => c.ColumnName.ToLower() == colName.ToLower());
            if (dataColumn == null)
            {
                return;
            }
            var columnName = dataColumn.ColumnName;
            using (DataColumn dc = new DataColumn($"{columnName}_new", dataType))
            {
                int ordinal = dataColumn.Ordinal;
                dt.Columns.Add(dc);
                dc.SetOrdinal(ordinal);
                foreach (DataRow dr in dt.Rows)
                {
                    dr[dc.ColumnName] = dr[columnName] == DBNull.Value ? DBNull.Value : Convert.ChangeType(dr[columnName], dataType);
                }
                dt.Columns.Remove(columnName);
                dc.ColumnName = columnName;
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

            string searchDialogFilter = AddSearchDialogFilterPart(gridModel);
            if (string.IsNullOrEmpty(searchDialogFilter) == false)
            {
                filterParts.Add(searchDialogFilter);
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

            /*
            if (gridModel.IsNested || gridModel.IsLinked)
            {
                if (!string.IsNullOrEmpty(gridModel.ParentKey))
                {
                    filterParts.Add(KeyFilter(gridModel, gridModel.GetColumns().Where(c => c.ForeignKey).ToList()));
                }
                else
                {
                    filterParts.Add("(1=2)");
                }
            }
            */

            if (!string.IsNullOrEmpty(gridModel.FixedFilter))
            {
                filterParts.Add($"({gridModel.FixedFilter})");
            }

            return String.Join(" and ", filterParts);
        }

        public static string AddSearchDialogFilterPart(this ComponentModel componentModel)
        {
            List<string> filterParts = new List<string>();

            foreach (var searchFilterPart in componentModel.SearchDialogFilter)
            {
                ColumnModel columnModel = componentModel.GetColumns().First(c => c.Key == searchFilterPart.ColumnKey);
                string filterExpression = FilterExpression(searchFilterPart, columnModel, componentModel);
                if (string.IsNullOrEmpty(filterExpression) == false)
                {
                    filterParts.Add(filterExpression);
                }
            }
            return string.Join($" {componentModel.SearchDialogConjunction} ", filterParts);
        }

        private static string FilterExpression(SearchDialogFilter searchDialogFilter, ColumnModel columnModel, ComponentModel componentModel)
        {
            string template = searchDialogFilter.Operator.GetAttribute<FilterExpressionAttribute>()?.Expression ?? string.Empty;
            string columnName = TextHelper.DelimitColumn(columnModel.Name, componentModel.DataSourceType);

            if (template == string.Empty)
            {
                return template;
            }

            List<string> values = new List<string>();
            string quotedValue1 = QuotedValue(searchDialogFilter.Value1);
            string quotedValue2 = QuotedValue(searchDialogFilter.Value2);

            switch (searchDialogFilter.Operator)
            {
                case SearchOperator.In:
                case SearchOperator.NotIn:
                    foreach (string value in searchDialogFilter.Value1.Split(","))
                    {
                        values.Add(QuotedValue(value));
                    }
                    return $"{columnName} {template.Replace("{0}", $"{Quoted(columnModel)}{string.Join(",", values)}{Quoted(columnModel)}")}";
                case SearchOperator.IsEmpty:
                case SearchOperator.IsNotEmpty:
                    return $"{columnName} {template}";
                case SearchOperator.Between:
                    return $"({columnName} >= {quotedValue1} and {columnName} <= {quotedValue2})";
                case SearchOperator.NotBetween:
                    return $"({columnName} < {quotedValue1} or {columnName} > {quotedValue2})";
                default:
                    return $"{columnName} {template.Replace("{0}", QuotedValue(WildcardValue(searchDialogFilter.Operator, quotedValue1.Replace("'", ""))))}";
            }

            string QuotedValue(string value)
            {
                return $"{Quoted(columnModel)}{value}{Quoted(columnModel)}";
            }

            string WildcardValue(SearchOperator searchOperator, string value)
            {
                string template = string.Empty;
                switch (searchOperator)
                {
                    case SearchOperator.Contains:
                    case SearchOperator.DoesNotContain:
                        template = "%{0}%";
                        break;
                    case SearchOperator.StartsWith:
                    case SearchOperator.DoesNotStartWith:
                        template = "{0}%";
                        break;
                    case SearchOperator.EndsWith:
                    case SearchOperator.DoesNotEndWith:
                        template = "%{0}";
                        break;
                }

                if (string.IsNullOrEmpty(template))
                {
                    return value;
                }
                return string.Format(template, value);
            }
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

            /*
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
            */

            if (!string.IsNullOrEmpty(selectModel.FixedFilter))
            {
                filterParts.Add($"({selectModel.FixedFilter})");
            }

            return String.Join(" and ", filterParts);
        }

        private static string AddPrimaryKeyFilter(ComponentModel componentModel)
        {
            return KeyFilter(componentModel, componentModel.GetColumns().Where(c => c.PrimaryKey).ToList());
        }

        private static string KeyFilter(ComponentModel componentModel, List<ColumnModel> keyColumns)
        {
            List<string> primaryKeyFilter = new List<string>();
            /*
            List<object> primaryKeyValues = TextHelper.DeobfuscateKey<List<object>>(componentModel.ParentKey) ?? new List<object>();
            if (primaryKeyValues.Count() == keyColumns.Count())
            {
                foreach (var item in keyColumns.Select((value, index) => new { value = value, index = index }))
                {
                    primaryKeyFilter.Add($"({item.value.Name} = {Quoted(item.value)}{primaryKeyValues[item.index]}{Quoted(item.value)})");
                }
            }
            */
            return string.Join(" and ", primaryKeyFilter);
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
