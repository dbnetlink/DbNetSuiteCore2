using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Enums;
using System.Data;
using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.Extensions
{
    public static class GridModelExtensions
    {
        public static QueryCommandConfig BuildEmptyQuery(this GridModel gridModel)
        {
            return new QueryCommandConfig(gridModel.DataSourceType) { Sql = $"select {GetColumnExpressions(gridModel)} from {gridModel.TableName} where 1=2"};
        }

        private static string GetColumnExpressions(this GridModel gridModel)
        {
            return ColumnsHelper.GetColumnExpressions(gridModel.Columns.Cast<ColumnModel>());
        }

        public static void AddFilterPart(this GridModel gridModel, QueryCommandConfig query)
        {
            List<string> filterParts = new List<string>();

            string filter = ComponentModelExtensions.AddSearchInputFilterPart(gridModel, query);

            if (string.IsNullOrEmpty(filter) == false)
            {
                filterParts.Add(filter);
            }

            string searchDialogFilter = ComponentModelExtensions.AddSearchDialogFilterPart(gridModel, query);
            if (string.IsNullOrEmpty(searchDialogFilter) == false)
            {
                filterParts.Add(searchDialogFilter);
            }

            List<string> columnFilterParts = ColumnFilterParts(gridModel, query);

            if (columnFilterParts.Any())
            {
                filterParts.Add($"({string.Join(" and ", columnFilterParts)})");
            }

            if (gridModel.IsNested || gridModel.IsLinked)
            {
                ComponentModelExtensions.AddParentKeyFilterPart(gridModel, query, filterParts);
            }

            if (!string.IsNullOrEmpty(gridModel.FixedFilter))
            {
                filterParts.Add($"({gridModel.FixedFilter})");
                ComponentModelExtensions.AssignParameters(query, gridModel.FixedFilterParameters);
            }

            if (filterParts.Any())
            {
                query.Sql += $" where ({string.Join(") and (", filterParts)})";
            }
        }

        public static void AddGroupByPart(this GridModel gridModel, QueryCommandConfig query)
        {
            if (gridModel.Columns.Any(c => c.Aggregate != AggregateType.None) == false)
            {
                return;
            }
            query.Sql += $" group by {string.Join(",", gridModel.Columns.Where(c => c.Aggregate == AggregateType.None).Select(c => c.Expression).ToList())}";
        }

        public static void AddHavingPart(this GridModel gridModel, QueryCommandConfig query)
        {
            List<string> havingParts = new List<string>();

            List<string> columnFilterParts = ColumnFilterParts(gridModel, query, true);

            if (columnFilterParts.Any())
            {
                havingParts.Add($"({string.Join(" and ", columnFilterParts)})");
            }

            string searchDialogFilter = ComponentModelExtensions.AddSearchDialogFilterPart(gridModel, query, true);
            if (string.IsNullOrEmpty(searchDialogFilter) == false)
            {
                havingParts.Add(searchDialogFilter);
            }

            if (havingParts.Any())
            {
                query.Sql += $" having {string.Join(" and ", havingParts)}";
            }
        }

        private static List<string> ColumnFilterParts(this GridModel gridModel, CommandConfig query, bool havingFilter = false)
        {
            if (gridModel.FilterColumns.Any() == false)
            {
                return new List<string>();
            }
            List<string> columnFilterParts = new List<string>();
            for (var i = 0; i < gridModel.ColumnFilter.Count; i++)
            {
                if (string.IsNullOrEmpty(gridModel.ColumnFilter[i]))
                {
                    continue;
                }

                var column = gridModel.Columns.Where(c => c.Filter != FilterType.None).Skip(i).First();
                if (column.Aggregate == AggregateType.None == havingFilter)
                {
                    continue;
                }

                var columnFilter = ParseFilterColumnValue(gridModel.ColumnFilter[i], column);

                if (columnFilter != null)
                {
                    string expression = FilterColumnExpression(gridModel, column, havingFilter);
                    object? paramValue = ComponentModelExtensions.ParamValue(columnFilter.Value.Value, column, gridModel.DataSourceType, true);
                   
                    if (string.IsNullOrEmpty(paramValue?.ToString()))
                    {
                        column.FilterError = paramValue == null ? ResourceHelper.GetResourceString(ResourceNames.DataFormatError) : ResourceHelper.GetResourceString(ResourceNames.ColumnFilterNoData);
                        continue;
                    }

                    if (columnFilter.Value.Key == "like")
                    {
                        paramValue = paramValue.ToString().ToLower();
                        expression = ComponentModelExtensions.CaseInsensitiveExpression(gridModel, expression);
                    }
                    string paramName = DbHelper.ParameterName($"columnfilter{i}", gridModel.DataSourceType);
                    query.Params[paramName] = paramValue;

                    paramName = ComponentModelExtensions.UpdateParamName(paramName, column, gridModel.DataSourceType);
                    columnFilterParts.Add($"{expression} {columnFilter.Value.Key} {paramName}");
                }
                else
                {
                    column.FilterError = ResourceHelper.GetResourceString(ResourceNames.DataFormatError);
                }
            }

            return columnFilterParts;
        }

        private static string FilterColumnExpression(GridModel gridModel, GridColumn gridColumnModel, bool havingFilter)
        {
            if (havingFilter == false)
            {
                return ComponentModelExtensions.RefineSearchExpression(gridColumnModel, gridModel);
            }

            switch (gridModel.DataSourceType)
            {
                case DataSourceType.SQLite:
                    return gridColumnModel.ColumnName;
                default:
                    return ComponentModelExtensions.AggregateExpression(gridColumnModel);
            }
        }

        public static void AddOrderPart(this GridModel gridModel, QueryCommandConfig query)
        {
            if (string.IsNullOrEmpty(gridModel.SortColumnOrdinal))
            {
                return;
            }
            query.Sql += $" order by {gridModel.SortColumnOrdinal} {gridModel.SortSequence}";
        }

        public static string AddDataTableOrderPart(this GridModel gridModel)
        {
            if (string.IsNullOrEmpty(gridModel.SortColumnName))
            {
                return string.Empty;
            }
            return $"{gridModel.SortColumnName} {gridModel.SortSequence}";
        }

        public static KeyValuePair<string, object>? ParseFilterColumnValue(string filterColumnValue, GridColumn gridColumn)
        {
            string comparisionOperator = "=";

            if (filterColumnValue.StartsWith(">=") || filterColumnValue.StartsWith("<="))
            {
                comparisionOperator = filterColumnValue.Substring(0, 2);
            }
            else if (filterColumnValue.StartsWith("<>") || filterColumnValue.StartsWith("!="))
            {
                comparisionOperator = filterColumnValue.Substring(0, 2);
            }
            else if (filterColumnValue.StartsWith(">") || filterColumnValue.StartsWith("<"))
            {
                comparisionOperator = filterColumnValue.Substring(0, 1);
            }

            if (comparisionOperator != "=")
            {
                filterColumnValue = filterColumnValue.Substring(comparisionOperator.Length);
            }

            if (string.IsNullOrEmpty(filterColumnValue))
            {
                return new KeyValuePair<string, object>(comparisionOperator, string.Empty);
            }

            if (gridColumn.IsNumeric || gridColumn.LookupOptions != null)
            {
                return new KeyValuePair<string, object>(comparisionOperator, filterColumnValue);
            }
            switch (gridColumn.DataTypeName)
            {
                case nameof(Boolean):
                    return new KeyValuePair<string, object>("=", ComponentModelExtensions.ParseBoolean(filterColumnValue));
                case nameof(DateTime):
                    try
                    {
                        return new KeyValuePair<string, object>(comparisionOperator, Convert.ToDateTime(filterColumnValue));
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                case nameof(TimeSpan):
                    try
                    {
                        return new KeyValuePair<string, object>(comparisionOperator, TimeSpan.Parse(filterColumnValue ?? string.Empty));
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                default:
                    return new KeyValuePair<string, object>("like", $"%{filterColumnValue}%");
            }
        }

        public static void GetDistinctLookups(this GridModel gridModel)
        {
            if (gridModel.Data.Rows.Count > 0)
            {
                foreach (var gridColumn in gridModel.Columns.Where(c => c.DistinctLookup))
                {
                    DataColumn? dataColumn = gridModel.GetDataColumn(gridColumn);
                    var lookupValues = gridModel.Data.DefaultView.ToTable(true, dataColumn.ColumnName).Rows.Cast<DataRow>().Where(dr => string.IsNullOrEmpty(dr[0]?.ToString()) == false && dr[0] != DBNull.Value).Select(dr => Convert.ChangeType(dr[0], gridColumn.DataType)).OrderBy(v => v).ToList();
                    gridColumn.DbLookupOptions = lookupValues.AsEnumerable().OrderBy(v => v).Select(v => new KeyValuePair<string, string>(v.ToString() ?? string.Empty, v.ToString() ?? string.Empty)).ToList();
                }
            }
        }
    }
}