﻿using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Data;
using System.Globalization;
using DbNetSuiteCore.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DbNetSuiteCore.Extensions
{
    public static class ComponentModelExtensions
    {
        public static QueryCommandConfig BuildEmptyQuery(this ComponentModel componentModel)
        {
            return new QueryCommandConfig($"select {ColumnsHelper.GetColumnExpressions(componentModel.GetColumns())} from {componentModel.TableName} where 1=2");
        }

        public static QueryCommandConfig BuildQuery(this ComponentModel componentModel)
        {
            string sql = $"select {Distinct(componentModel)}{Top(componentModel)}{AddSelectPart(componentModel)} from {componentModel.TableName}";
            QueryCommandConfig query = new QueryCommandConfig(sql);

            if (componentModel is GridModel)
            {
                var gridModel = (GridModel)componentModel;
                gridModel.AddFilterPart(query);
                gridModel.AddGroupByPart(query);
                gridModel.AddHavingPart(query);
                gridModel.AddOrderPart(query);
            }

            if (componentModel is SelectModel)
            {
                var selectModel = (SelectModel)componentModel;
                selectModel.AddFilterPart(query);
                selectModel.AddOrderPart(query);
            }

            if (componentModel is FormModel)
            {
                var formModel = (FormModel)componentModel;
                formModel.AddFilterPart(query);
                formModel.AddOrderPart(query);
            }

            query.Sql = $"{query.Sql}{Limit(componentModel)}";
            return query;
        }

        public static QueryCommandConfig BuildRecordQuery(this ComponentModel componentModel, object? primaryKeyValue = null)
        {
            string sql = $"select {AddSelectPart(componentModel, true)} from {componentModel.TableName}";
            QueryCommandConfig query = new QueryCommandConfig(sql);
            AddPrimaryKeyFilterPart(componentModel, query, primaryKeyValue == null ? componentModel.ParentKey : primaryKeyValue);
            return query;
        }

        public static string AddSearchInputFilterPart(this ComponentModel componentModel, QueryCommandConfig query)
        {
            List<string> filterParts = new List<string>();

            if (!string.IsNullOrEmpty(componentModel.SearchInput))
            {
                foreach (var column in componentModel.SearchableColumns)
                {
                    ComponentModelExtensions.AddSearchFilterPart(componentModel, column, query, filterParts);
                }

                foreach (var column in componentModel.GetColumns().Where(c => c.Lookup != null && string.IsNullOrEmpty(c.Lookup.TableName) == false))
                {
                    query.Params[$"@{column.ParamName}"] = $"%{componentModel.SearchInput}%";
                    var lookupSql = $"select {column.Lookup?.KeyColumn} from {column.Lookup?.TableName} where {column.Lookup?.DescriptionColumn} like @{column.ParamName}";
                    filterParts.Add($"{RefineSearchExpression(column, componentModel)} in ({lookupSql})");
                }

            }
            return string.Join(" or ", filterParts);
        }

        public static string AddSearchDialogFilterPart(this ComponentModel componentModel, QueryCommandConfig query, bool havingFilter = false)
        {
            List<string> filterParts = new List<string>();

            foreach (var searchFilterPart in componentModel.SearchDialogFilter)
            {
                ColumnModel colummnModel = componentModel.GetColumns().First(c => c.Key == searchFilterPart.ColumnKey);

                if (colummnModel is GridColumn)
                {
                    GridColumn gridColumn = (GridColumn)colummnModel;
                    if (gridColumn.Aggregate == AggregateType.None == havingFilter)
                    {
                        continue;
                    }
                }

                string filterExpression = FilterExpression(searchFilterPart, query, componentModel, colummnModel);
                if (string.IsNullOrEmpty(filterExpression) == false)
                {
                    filterParts.Add($"{RefineSearchExpression(colummnModel, componentModel)} {filterExpression}");
                }
            }
            return string.Join($" {componentModel.SearchDialogConjunction} ", filterParts);
        }

        private static string FilterExpression(SearchDialogFilter searchDialogFilter, QueryCommandConfig query, ComponentModel componentModel, ColumnModel columnModel)
        {
            string template = searchDialogFilter.Operator.GetAttribute<FilterExpressionAttribute>()?.Expression ?? string.Empty;

            if (template == string.Empty)
            {
                return template;
            }
            List<string> parameterNames = new List<string>();

            switch (searchDialogFilter.Operator)
            {
                case SearchOperator.In:
                case SearchOperator.NotIn:
                    foreach(string paramValue in searchDialogFilter.Value1.Split(","))
                    {
                        parameterNames.Add(ParameterName(searchDialogFilter.ColumnKey, parameterNames.Count));
                        query.Params[parameterNames.Last()] = ParamValue(paramValue, columnModel, componentModel.DataSourceType) ?? DBNull.Value;
                    }
                    return template.Replace("{0}", string.Join(",", parameterNames));
                case SearchOperator.IsEmpty:
                case SearchOperator.IsNotEmpty:
                    return template;
                case SearchOperator.Between:
                case SearchOperator.NotBetween:
                    object value1 = ParamValue(searchDialogFilter.Value1, columnModel, componentModel.DataSourceType) ?? DBNull.Value;
                    object value2 = ParamValue(searchDialogFilter.Value2, columnModel, componentModel.DataSourceType) ?? DBNull.Value;
                    foreach ( int i in Enumerable.Range(0,2))
                    {
                        parameterNames.Add(ParameterName(searchDialogFilter.ColumnKey, parameterNames.Count));
                        query.Params[parameterNames.Last()] = (i == 0 ? value1 : value2) ?? string.Empty;
                    }
                    return template.Replace("{0}", parameterNames[0]).Replace("{1}", parameterNames[1]);
                default:
                    object value = ParamValue(searchDialogFilter.Value1, columnModel, componentModel.DataSourceType) ?? DBNull.Value;
                    var paramName = ParameterName(searchDialogFilter.ColumnKey, 0);
                    query.Params[paramName] = SearchFilterParam(searchDialogFilter.Operator, value) ?? string.Empty;
                    return template.Replace("{0}", paramName);
            }

            string ParameterName(string columnKey, int idx)
            {
                return DbHelper.ParameterName($"sd_{columnKey}{idx}");
            }
        }

        private static object? SearchFilterParam(SearchOperator searchOperator, object? value)
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
                case SearchOperator.True:
                    value = true;
                    break;
                case SearchOperator.False:
                    value = false;
                    break;
            }

            if (string.IsNullOrEmpty(template))
            {
                return value;
            }

            return string.Format(template, value?.ToString());
        }

        public static string RefineSearchExpression(ColumnModel col, ComponentModel componentModel)
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

            switch (col.DataTypeName) 
            {
                case nameof(DateTime):
                    switch (componentModel.DataSourceType)
                    {
                        case DataSourceType.MSSQL:
                            if (col.DbDataType != nameof(MSSQLDataTypes.Date)) 
                            {
                                columnExpression = $"CONVERT(DATE,{columnExpression})";
                            }
                            break;
                        case DataSourceType.SQLite:
                            columnExpression = $"DATE({columnExpression})";
                            break;
                    }
                    break;
                case nameof(TimeSpan):
                    switch (componentModel.DataSourceType)
                    {
                        case DataSourceType.MSSQL:
                            if (col.DbDataType == nameof(MSSQLDataTypes.Time))
                            {
                                columnExpression = $"cast(Format({columnExpression}, N'hh\\:mm') as time)";
                            }
                            break;
                    }
                    break;
                default:
                    break;
            }

            return columnExpression;
        }

        private static void AddPrimaryKeyFilterPart(ComponentModel componentModel, CommandConfig query, object primaryKeyValue)
        {
            var primaryKeyColumn = componentModel.GetColumns().FirstOrDefault(c => c.PrimaryKey);
            query.Sql += $" where {primaryKeyColumn.Expression} = @{primaryKeyColumn.ParamName}";
            query.Params[$"@{primaryKeyColumn.ParamName}"] = ColumnModelHelper.TypedValue(primaryKeyColumn, primaryKeyValue) ?? string.Empty;
        }

        public static void AddParentKeyFilterPart(ComponentModel componentModel, CommandConfig query, List<string> filterParts)
        {
            if (!string.IsNullOrEmpty(componentModel.ParentKey))
            {
                var foreignKeyColumn = componentModel.GetColumns().FirstOrDefault(c => c.ForeignKey);
                if (foreignKeyColumn != null)
                {
                    filterParts.Add($"({DbHelper.StripColumnRename(foreignKeyColumn.Expression)} = @{foreignKeyColumn.ParamName})");
                    query.Params[$"@{foreignKeyColumn.ParamName}"] = ColumnModelHelper.TypedValue(foreignKeyColumn, componentModel.ParentKey) ?? string.Empty;
                }
            }
            else
            {
                filterParts.Add($"(1=2)");
            }
        }

        public static QueryCommandConfig BuildProcedureCall(this ComponentModel componentModel)
        {
            QueryCommandConfig query = new QueryCommandConfig($"{componentModel.ProcedureName}");
            AssignParameters(query, componentModel.ProcedureParameters);
            return query;
        }

        public static void AssignParameters(QueryCommandConfig query, List<DbParameter> parameters)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.Value is JsonElement)
                {
                    parameter.Value = JsonElementExtension.Value((JsonElement)parameter.Value);
                }
                query.Params[DbHelper.ParameterName(parameter.Name)] = ColumnModelHelper.TypedValue(parameter.TypeName, parameter.Value);
            }
        }

        public static string AddSelectPart(this ComponentModel componentModel, bool recordQuery = false)
        {
            if (componentModel.GetColumns().Any() == false)
            {
                return "*";
            }

            if (componentModel is FormModel && recordQuery == false)
            {
                return string.Join(",", componentModel.GetColumns().Where(c => c.PrimaryKey).Select(c => c.Expression));
            }

            List<string> selectPart = new List<string>();

            foreach (var column in componentModel.GetColumns())
            {
                var columnExpression = column.Expression;

                if (column is GridColumn)
                {
                    var gridColumn = (GridColumn)column;
                    if (gridColumn.Aggregate != AggregateType.None)
                    {
                        columnExpression = $"{AggregateExpression(gridColumn)} as {column.ColumnName}";
                    }
                }

                selectPart.Add(columnExpression);
            }

            return string.Join(",", selectPart);
        }

        public static string AggregateExpression(GridColumn c)
        {
            return $"{c.Aggregate}({Regex.Replace(c.Expression, @" as \w*$", "", RegexOptions.IgnoreCase)})";
        }

        public static void AddSearchFilterPart(ComponentModel componentModel, ColumnModel columnModel, QueryCommandConfig query, List<string> filterParts)
        {
            string searchInput = componentModel.SearchInput.ToLower();
            string expression = DbHelper.StripColumnRename(columnModel.Expression);
            expression = CaseInsensitiveExpression(componentModel, expression);
            query.Params[$"@{columnModel.ParamName}"] = $"%{searchInput}%";
            filterParts.Add($"{expression} like @{columnModel.ParamName}");
        }

        public static string CaseInsensitiveExpression(ComponentModel componentModel, string expression)
        {
            if (IsCsvFile(componentModel))
            {
                expression = $"LCASE({expression})";
            }

            switch (componentModel.DataSourceType)
            {
                case DataSourceType.PostgreSql:
                case DataSourceType.MySql:
                    expression = $"LOWER({expression})";
                    break;
            }
            return expression;
        }

        public static void ConvertEnumLookups(this ComponentModel componentModel)
        {
            foreach (var column in componentModel.GetColumns().Where(c => c.LookupOptions != null && c.Lookup == null))
            {
                DataColumn? dataColumn = componentModel.GetDataColumn(column);
                componentModel.Data.ConvertLookupColumn(dataColumn, column, componentModel);
            }
        }

        public static bool IsCsvFile(ComponentModel componentModel)
        {
            return componentModel.DataSourceType == DataSourceType.Excel && componentModel.TableName.ToLower().Replace("]", string.Empty).EndsWith(".csv");
        }
        public static string Top(ComponentModel componentModel)
        {
            switch (componentModel.DataSourceType)
            {
                case DataSourceType.MSSQL:
                    return QueryLimit(componentModel);
            }

            return string.Empty;
        }

        public static string Distinct(ComponentModel componentModel)
        {
            return componentModel.Distinct ? "distinct " : string.Empty;
        }

        public static object? ParamValue(object value, ColumnModel column, DataSourceType dataSourceType, bool gridColumnFilter = false)
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
                        if (gridColumnFilter == false)
                        {
                            if (column is FormColumn)
                            {
                                string inputType = (column as FormColumn).ControlType.ToString();
                                paramValue = TimeSpan.ParseExact(value.ToString(), column.GetDateTimeFormat(inputType), CultureInfo.CurrentCulture, TimeSpanStyles.None);
                            }
                            if (column is GridColumn)
                            {
                                string inputType = (column as GridColumn).SearchControlType.ToString();
                                paramValue = TimeSpan.ParseExact(value.ToString(), column.GetDateTimeFormat(inputType), CultureInfo.CurrentCulture, TimeSpanStyles.None);
                            }
                        }
                        else
                        {
                            paramValue = TimeSpan.Parse(value.ToString(), CultureInfo.CurrentCulture);
                        }
                        break;
                    case nameof(DateTime):
                        if (gridColumnFilter == false)
                        {
                            if (column is FormColumn)
                            {
                                string inputType = (column as FormColumn).ControlType.ToString();
                                paramValue = DateTime.ParseExact(value.ToString(), column.GetDateTimeFormat(inputType), CultureInfo.CurrentCulture, DateTimeStyles.None);
                            }
                            if (column is GridColumn)
                            {
                                string inputType = (column as GridColumn).SearchControlType.ToString();
                                paramValue = DateTime.ParseExact(value.ToString(), column.GetDateTimeFormat(inputType), CultureInfo.CurrentCulture, DateTimeStyles.None);
                            }
                        }
                        else
                        {
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
                            if (paramValue is DateTime && dataSourceType == DataSourceType.MSSQL)
                            {
                                int year = ((DateTime)paramValue).Year;
                                if (year < 1753 || year > 9999)
                                {
                                    return null;
                                }
                            }
                        }
                        break;
                    case nameof(DateTimeOffset):
                        if (column is FormColumn)
                        {
                            paramValue = DateTimeOffset.ParseExact(value.ToString(), (column as FormColumn).DateTimeFormat, CultureInfo.CurrentCulture, DateTimeStyles.None);
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(column.Format))
                            {
                                paramValue = Convert.ChangeType(value, Type.GetType($"System.{nameof(DateTimeOffset)}"));
                            }
                            else
                            {
                                try
                                {
                                    paramValue = DateTimeOffset.ParseExact(value.ToString(), column.Format, CultureInfo.CurrentCulture);
                                }
                                catch
                                {
                                    paramValue = DateTimeOffset.Parse(value.ToString(), CultureInfo.CurrentCulture);
                                }
                            }
                            if (paramValue is DateTimeOffset && dataSourceType == DataSourceType.MSSQL)
                            {
                                int year = ((DateTimeOffset)paramValue).Year;
                                if (year < 1753 || year > 9999)
                                {
                                    return null;
                                }
                            }
                        }
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
            }

            switch (dataType)
            {
                case nameof(DateTime):
                    switch (dataSourceType)
                    {
                        case DataSourceType.SQLite:
                            paramValue = Convert.ToDateTime(paramValue).ToString("yyyy-MM-dd");
                            break;
                    }
                    break;
            }

            return paramValue;
        }

        public static bool ParseBoolean(object boolString)
        {
            switch ((boolString?.ToString() ?? string.Empty).ToLower())
            {
                case "on":
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

        public static string Limit(ComponentModel componentModel)
        {
            switch (componentModel.DataSourceType)
            {
                case DataSourceType.MySql:
                case DataSourceType.PostgreSql:
                case DataSourceType.SQLite:
                    return QueryLimit(componentModel);
            }

            return string.Empty;
        }

        private static string QueryLimit(ComponentModel componentModel)
        {
            string limit = string.Empty;
            if (componentModel.QueryLimit > 0)
            {
                switch (componentModel.DataSourceType)
                {
                    case DataSourceType.MSSQL:
                        limit = $"TOP {componentModel.QueryLimit} ";
                        break;
                    case DataSourceType.MySql:
                    case DataSourceType.PostgreSql:
                    case DataSourceType.SQLite:
                        limit = $" LIMIT {componentModel.QueryLimit}";
                        break;
                }
            }

            return limit;
        }

        public static string UpdateParamName(string paramName, ColumnModel column, DataSourceType dataSourceType)
        {
            if (dataSourceType == DataSourceType.PostgreSql)
            {
                if (column.DbDataType == PostgreSqlDataTypes.Enum.ToString())
                {
                    paramName = $"CAST({paramName} as \"{column.EnumName.Split(".").Last()}\")";
                }
            }

            return paramName;
        }
    }
}