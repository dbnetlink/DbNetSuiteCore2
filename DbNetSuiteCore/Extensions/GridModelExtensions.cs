using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Enums;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Data;
using System.Linq;
using System.Text.Json;

namespace DbNetSuiteCore.Extensions
{
    public static class GridModelExtensions
    {
        public static QueryCommandConfig BuildQuery(this GridModel gridModel, DbRepository dbRepository)
        {
            string sql = $"select {AddSelectPart(gridModel)} from {gridModel.TableName}";
            QueryCommandConfig query = new QueryCommandConfig(sql);

            gridModel.AddFilterPart(query, dbRepository);
            gridModel.AddGroupByPart(query);
            gridModel.AddHavingPart(query);
            gridModel.AddOrderPart(query);

            return query;
        }

        public static QueryCommandConfig BuildProcedureCall(this GridModel gridModel, DbRepository dbRepository)
        {
            QueryCommandConfig query = new QueryCommandConfig($"{gridModel.ProcedureName}");
            foreach(var parameter in gridModel.ProcedureParameters)
            {
                if (parameter.Value is JsonElement)
                {
                    parameter.Value = JsonElementExtension.Value((JsonElement)parameter.Value);
                }
                query.Params[DbRepository.ParameterName(parameter.Name)] = GridColumnModelExtensions.TypedValue(parameter.TypeName, parameter.Value);
            }
            
            return query;
        }

        public static QueryCommandConfig BuildEmptyQuery(this GridModel gridModel)
        {
            return new QueryCommandConfig($"select {GetColumnExpressions(gridModel)} from {gridModel.TableName} where 1=2");
        }

        private static string AddSelectPart(this GridModel gridModel)
        {
            if (gridModel.Columns.Any() == false)
            {
                return "*";
            }

            List<string> selectPart = new List<string>();

            foreach (var gridColumn in gridModel.Columns)
            {
                var columnExpression = gridColumn.Expression;
                if (gridColumn.Aggregate != AggregateType.None)
                {
                    columnExpression = $"{AggregateExpression(gridColumn)} as {gridColumn.ColumnName}";
                }
                selectPart.Add(columnExpression);
            }
            return string.Join(",", selectPart);
        }

        private static string GetColumnExpressions(this GridModel gridModel)
        {
            return gridModel.Columns.Any() ? string.Join(",", gridModel.Columns.Select(x => x.Expression).ToList()) : "*";
        }


        private static void AddFilterPart(this GridModel gridModel, CommandConfig query, DbRepository dbRepository)
        {
            List<string> filterParts = new List<string>();

            if (!string.IsNullOrEmpty(gridModel.SearchInput))
            {
                List<string> quickSearchFilterPart = new List<string>();

                foreach (var gridColumn in gridModel.Columns.Where(c => c.Searchable))
                {
                    query.Params[$"@{gridColumn.ParamName}"] = $"%{gridModel.SearchInput}%";
                    quickSearchFilterPart.Add($"{RefineSearchExpression(gridColumn, gridModel)} like @{gridColumn.ParamName}");
                }

                foreach (var gridColumn in gridModel.Columns.Where(c => c.Lookup != null))
                {
                    var lookupValues = dbRepository.GetLookupKeys(gridModel, gridColumn).Result;
                    var paramNames = Enumerable.Range(1, lookupValues.Count).Select(i => DbRepository.ParameterName($"{gridColumn.Name}lookupparam{i}")).ToList();

                    int i = 0;
                    paramNames.ForEach(p => query.Params[p] = lookupValues[i++]);
                    quickSearchFilterPart.Add($"{RefineSearchExpression(gridColumn, gridModel)}  in ({String.Join(",", paramNames)})");
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
                        filterParts.Add($"({foreignKeyColumn.Expression.Split(" ").First()} = @{foreignKeyColumn.ParamName})");
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
            }

            if (filterParts.Any())
            {
                query.Sql += $" where {string.Join(" and ", filterParts)}";
            }
        }

        private static void AddGroupByPart(this GridModel gridModel, QueryCommandConfig query)
        {
            if (gridModel.Columns.Any(c => c.Aggregate != AggregateType.None) == false)
            {
                return;
            }
            query.Sql += $" group by {string.Join(",", gridModel.Columns.Where(c => c.Aggregate == AggregateType.None).Select(c => c.Expression).ToList())}";
        }

        private static void AddHavingPart(this GridModel gridModel, CommandConfig query)
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
            if (gridModel.Columns.Any(c => c.Filter) == false)
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

                var column = gridModel.Columns.Skip(i).First();

                if (column.Aggregate == AggregateType.None == havingFilter)
                {
                    continue;
                }

                var columnFilter = ParseFilterColumnValue(gridModel.ColumnFilter[i], column);

                if (columnFilter != null)
                {
                    columnFilterParts.Add($"{FilterColumnExpression(gridModel, column, havingFilter)} {columnFilter.Value.Key} @columnfilter{i}");
                    query.Params[$"@columnfilter{i}"] = ParamValue(columnFilter.Value.Value, column, gridModel);
                }
            }

            return columnFilterParts;
        }

        private static string FilterColumnExpression(GridModel gridModel, GridColumnModel gridColumnModel, bool havingFilter)
        {
            if (havingFilter == false)
            {
                return RefineSearchExpression(gridColumnModel, gridModel);
            }

            switch (gridModel.DataSourceType)
            {
                case DataSourceType.SQlite:
                    return gridColumnModel.ColumnName;
                default:
                    return AggregateExpression(gridColumnModel);
            }
        }

        private static void AddOrderPart(this GridModel gridModel, QueryCommandConfig query)
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

        public static void QualifyColumnExpressions(this GridModel gridModel)
        {
            gridModel.Columns.ToList().ForEach(c => c.Expression = QualifyExpression(c.Expression, gridModel.DataSourceType));
        }
        public static string QualifyExpression(string expression, DataSourceType dataSourceType)
        {
            return QualifyTemplate(dataSourceType).Replace("@", expression);
        }
        public static string QualifyTemplate(DataSourceType dataSourceType)
        {
            switch (dataSourceType)
            {
                case DataSourceType.MSSQL:
                case DataSourceType.SQlite:
                    return $"[@]";
                case DataSourceType.MySql:
                    return $"`@`";
                case DataSourceType.PostgreSql:
                    return $"\"@\"";
            }
            return "@";
        }

        private static string AggregateExpression(GridColumnModel c)
        {
            return $"{c.Aggregate}({Regex.Replace(c.Expression, @" as \w*$", "", RegexOptions.IgnoreCase)})";
        }

        public static KeyValuePair<string, object>? ParseFilterColumnValue(string filterColumnValue, GridColumnModel gridColumn)
        {
            string comparisionOperator = "=";

            if (filterColumnValue.StartsWith(">=") || filterColumnValue.StartsWith("<="))
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

        public static void ConvertEnumLookups(this GridModel gridModel)
        {
            foreach (var gridColumn in gridModel.Columns.Where(c => c.LookupOptions != null && c.Lookup == null))
            {
                DataColumn? dataColumn = gridModel.GetDataColumn(gridColumn);
                gridModel.Data.ConvertLookupColumn(dataColumn, gridColumn, gridModel);
            }
        }

        private static string RefineSearchExpression(GridColumnModel col, GridModel gridModel)
        {
            string columnExpression = StripColumnRename(col.Expression);

            if (col.Aggregate != AggregateType.None)
            {
                columnExpression = AggregateExpression(col);
            }

            if (col.DataType != typeof(DateTime))
            {
                return columnExpression;
            }

            switch (gridModel.DataSourceType)
            {
                case DataSourceType.MSSQL:
                    if (col.DbDataType != "31") // "Date"
                    {
                        columnExpression = $"CONVERT(DATE,{columnExpression})";
                    }
                    break;
                case DataSourceType.SQlite:
                    columnExpression = $"DATE({columnExpression})";
                    break;
            }

            return columnExpression;
        }

        private static string StripColumnRename(string columnExpression)
        {
            string[] columnParts = columnExpression.Split(')');
            columnParts[columnParts.Length - 1] = Regex.Replace(columnParts[columnParts.Length - 1], " as .*", "", RegexOptions.IgnoreCase);
            columnParts[0] = Regex.Replace(columnParts[0], "unique |distinct ", "", RegexOptions.IgnoreCase);

            return String.Join(")", columnParts);
        }

        private static object ParamValue(object value, GridColumnModel column, GridModel gridModel)
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
                throw new Exception($"{e.Message} => : Value: {value.ToString()} DataType:{dataType}");
            }

            switch (dataType)
            {
                case nameof(DateTime):
                    switch (gridModel.DataSourceType)
                    {
                        case DataSourceType.SQlite:
                            paramValue = Convert.ToDateTime(paramValue).ToString("yyyy-MM-dd");
                            break;
                    }
                    break;
            }

            return paramValue;
        }

        private static bool ParseBoolean(string boolString)
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
