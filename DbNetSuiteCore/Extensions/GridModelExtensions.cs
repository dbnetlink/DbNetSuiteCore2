using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Extensions;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Data;
using System.Text.Json;
using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.Extensions
{
    public static class GridModelExtensions
    {

        public static QueryCommandConfig BuildRecordQuery(this GridModel gridModel)
        {
            string sql = $"select {ComponentModelExtensions.AddSelectPart(gridModel)} from {gridModel.TableName}";
            QueryCommandConfig query = new QueryCommandConfig(sql);

            gridModel.AddPrimaryKeyFilterPart(query);
            return query;
        }


        public static QueryCommandConfig BuildEmptyQuery(this GridModel gridModel)
        {
            return new QueryCommandConfig($"select {GetColumnExpressions(gridModel)} from {gridModel.TableName} where 1=2");
        }

        private static string GetColumnExpressions(this GridModel gridModel)
        {
            return ColumnsHelper.GetColumnExpressions(gridModel.Columns.Cast<ColumnModel>());
        }

        public static void AddFilterPart(this GridModel gridModel, QueryCommandConfig query)
        {
            List<string> filterParts = new List<string>();

            if (!string.IsNullOrEmpty(gridModel.SearchInput))
            {
                List<string> quickSearchFilterPart = new List<string>();

                foreach (var gridColumn in gridModel.Columns.Where(c => c.Searchable))
                {
                    ComponentModelExtensions.AddSearchFilterPart(gridModel, gridColumn, query, quickSearchFilterPart);
                }

                foreach (var gridColumn in gridModel.Columns.Where(c => c.Lookup != null && string.IsNullOrEmpty(c.Lookup.TableName) == false))
                {
                    query.Params[$"@{gridColumn.ParamName}"] = $"%{gridModel.SearchInput}%";
                    var lookupSql = $"select {gridColumn.Lookup.KeyColumn} from {gridColumn.Lookup.TableName} where {gridColumn.Lookup.DescriptionColumn} like @{gridColumn.ParamName}";
                    quickSearchFilterPart.Add($"{RefineSearchExpression(gridColumn, gridModel)} in ({lookupSql})");
                }

                if (quickSearchFilterPart.Any())
                {
                    filterParts.Add($"({string.Join(" or ", quickSearchFilterPart)})");
                }
            }

            List<string> columnFilterParts = ColumnFilterParts(gridModel, query);

            if (columnFilterParts.Any())
            {
                filterParts.Add($"({string.Join(" and ", columnFilterParts)})");
            }

            if (gridModel.IsNested || gridModel.IsLinked)
            {
                if (!string.IsNullOrEmpty(gridModel.ParentKey))
                {
                    var foreignKeyColumn = gridModel.Columns.FirstOrDefault(c => c.ForeignKey);
                    if (foreignKeyColumn != null)
                    {
                        filterParts.Add($"({DbHelper.StripColumnRename(foreignKeyColumn.Expression)} = @{foreignKeyColumn.ParamName})");
                        query.Params[$"@{foreignKeyColumn.ParamName}"] = foreignKeyColumn!.TypedValue(gridModel.ParentKey) ?? string.Empty;
                    }
                }
                else
                {
                    filterParts.Add($"(1=2)");
                }
            }

            if (!string.IsNullOrEmpty(gridModel.FixedFilter))
            {
                filterParts.Add($"({gridModel.FixedFilter})");
                ComponentModelExtensions.AssignParameters(query, gridModel.FixedFilterParameters);
            }

            if (filterParts.Any())
            {
                query.Sql += $" where {string.Join(" and ", filterParts)}";
            }
        }

        private static void AddPrimaryKeyFilterPart(this GridModel gridModel, CommandConfig query)
        {
            var primaryKeyColumn = gridModel.Columns.FirstOrDefault(c => c.PrimaryKey);
            query.Sql += $" where {primaryKeyColumn.Expression} = @{primaryKeyColumn.ParamName}";
            query.Params[$"@{primaryKeyColumn.ParamName}"] = primaryKeyColumn!.TypedValue(gridModel.ParentKey) ?? string.Empty;
        }

        public static void AddGroupByPart(this GridModel gridModel, QueryCommandConfig query)
        {
            if (gridModel.Columns.Any(c => c.Aggregate != AggregateType.None) == false)
            {
                return;
            }
            query.Sql += $" group by {string.Join(",", gridModel.Columns.Where(c => c.Aggregate == AggregateType.None).Select(c => c.Expression).ToList())}";
        }

        public static void AddHavingPart(this GridModel gridModel, CommandConfig query)
        {
            List<string> havingParts = new List<string>();

            List<string> columnFilterParts = ColumnFilterParts(gridModel, query, true);

            if (columnFilterParts.Any())
            {
                havingParts.Add($"({string.Join(" and ", columnFilterParts)})");
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
                    object? paramValue = ParamValue(columnFilter.Value.Value, column, gridModel);

                    if (paramValue is DateTime && gridModel.DataSourceType == DataSourceType.MSSQL)
                    {
                        int year = ((DateTime)paramValue).Year;
                        if (year < 1753 || year > 9999)
                        {
                            column.FilterError = ResourceHelper.GetResourceString(ResourceNames.ColumnFilterDataError);
                            continue;
                        }
                    }
                    if (string.IsNullOrEmpty(paramValue?.ToString()))
                    {
                        column.FilterError = paramValue == null ? ResourceHelper.GetResourceString(ResourceNames.ColumnFilterDataError) : ResourceHelper.GetResourceString(ResourceNames.ColumnFilterNoData);
                        continue;
                    }

                    if (ComponentModelExtensions.IsCsvFile(gridModel) && columnFilter.Value.Key == "like")
                    {
                        expression = $"LCASE({expression})";
                        paramValue = paramValue.ToString().ToLower();
                    }

                    columnFilterParts.Add($"{expression} {columnFilter.Value.Key} @columnfilter{i}");
                    query.Params[$"@columnfilter{i}"] = paramValue;
                }
                else
                {
                    column.FilterError = ResourceHelper.GetResourceString(ResourceNames.ColumnFilterDataError);
                }
            }

            return columnFilterParts;
        }

        private static string FilterColumnExpression(GridModel gridModel, GridColumn gridColumnModel, bool havingFilter)
        {
            if (havingFilter == false)
            {
                return RefineSearchExpression(gridColumnModel, gridModel);
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

            if (gridColumn.IsNumeric)
            {
                return new KeyValuePair<string, object>(comparisionOperator, filterColumnValue);
            }
            switch (gridColumn.DataTypeName)
            {
                case nameof(Boolean):
                    return new KeyValuePair<string, object>("=", ParseBoolean(filterColumnValue));
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
                foreach (var gridColumn in gridModel.Columns.Where(c => c.Lookup != null && string.IsNullOrEmpty(c.Lookup.TableName)))
                {
                    DataColumn? dataColumn = gridModel.GetDataColumn(gridColumn);
                    var lookupValues = gridModel.Data.DefaultView.ToTable(true, dataColumn.ColumnName).Rows.Cast<DataRow>().Where(dr => string.IsNullOrEmpty(dr[0]?.ToString()) == false).Select(dr => dr[0]).ToList();
                    gridColumn.DbLookupOptions = lookupValues.AsEnumerable().OrderBy(v => v).Select(v => new KeyValuePair<string, string>(v.ToString() ?? string.Empty, v.ToString() ?? string.Empty)).ToList();
                }
            }
        }

        private static string RefineSearchExpression(ColumnModel col, ComponentModel componentModel)
        {
            string columnExpression = DbHelper.StripColumnRename(col.Expression);


            if (col is GridColumn)
            {
                var gridCol = (GridColumn)col;
                if (gridCol.Aggregate != AggregateType.None)
                {
                    columnExpression = ComponentModelExtensions.AggregateExpression(gridCol);
                }
            }

            if (col.DataType != typeof(DateTime))
            {
                return columnExpression;
            }

            switch (componentModel.DataSourceType)
            {
                case DataSourceType.MSSQL:
                    if (col.DbDataType != "31") // "Date"
                    {
                        columnExpression = $"CONVERT(DATE,{columnExpression})";
                    }
                    break;
                case DataSourceType.SQLite:
                    columnExpression = $"DATE({columnExpression})";
                    break;
            }

            return columnExpression;
        }


        private static object? ParamValue(object value, GridColumn column, GridModel gridModel)
        {
            var dataType = column.DataTypeName;
            if (value == null)
            {
                if (dataType == "Byte[]")
                    return new byte[0];
                else
                    return DBNull.Value;
            }

            if (string.IsNullOrEmpty(value.ToString()))
            {
                return DBNull.Value;
            }

            object paramValue = value.ToString();
            try
            {
                switch (dataType)
                {
                    case nameof(String):
                        break;
                    case nameof(Boolean):
                        paramValue = ParseBoolean(value.ToString());
                        break;
                    case nameof(TimeSpan):
                        paramValue = TimeSpan.Parse(DateTime.Parse(value.ToString()).ToString(column.Format));
                        break;
                    case nameof(DateTime):
                        if (string.IsNullOrEmpty(column.Format))
                        {
                            paramValue = Convert.ChangeType(value, Type.GetType($"System.{nameof(DateTime)}"));
                        }
                        else
                        {
                            try
                            {
                                paramValue = DateTime.ParseExact(value.ToString(), column.Format, CultureInfo.CurrentCulture);
                            }
                            catch
                            {
                                paramValue = DateTime.Parse(value.ToString(), CultureInfo.CurrentCulture);
                            }
                        }
                        break;
                    case nameof(Byte):
                        paramValue = value;
                        break;
                    case nameof(Guid):
                        paramValue = new Guid(value.ToString());
                        break;
                    case nameof(Int16):
                    case nameof(Int32):
                    case nameof(Int64):
                    case nameof(Decimal):
                    case nameof(Single):
                    case nameof(Double):
                        if (string.IsNullOrEmpty(column.Format) == false)
                        {
                            var cultureInfo = Thread.CurrentThread.CurrentCulture;
                            value = value.ToString().Replace(cultureInfo.NumberFormat.CurrencySymbol, "");
                        }
                        paramValue = Convert.ChangeType(value, GetColumnType(dataType));
                        break;
                    case nameof(UInt16):
                    case nameof(UInt32):
                    case nameof(UInt64):
                        paramValue = Convert.ChangeType(value, GetColumnType(dataType.Replace("U", string.Empty)));
                        break;
                    default:
                        paramValue = Convert.ChangeType(value, GetColumnType(dataType));
                        break;
                }
            }
            catch (Exception e)
            {
                return null;
                //throw new Exception($"{e.Message} => : Value: {value.ToString()} DataType:{dataType}");
            }

            switch (dataType)
            {
                case nameof(DateTime):
                    switch (gridModel.DataSourceType)
                    {
                        case DataSourceType.SQLite:
                            paramValue = Convert.ToDateTime(paramValue).ToString("yyyy-MM-dd");
                            break;
                    }
                    break;
            }

            return paramValue;
        }

        public static bool ParseBoolean(string boolString)
        {
            switch (boolString.ToLower())
            {
                case "yes":
                case "true":
                case "1":
                    return true;
                default:
                    return false;
            }
        }
        private static Type GetColumnType(string typeName)
        {
            return Type.GetType("System." + typeName);
        }

    }
}