﻿using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Data;
using System.Globalization;

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
            }

            query.Sql = $"{query.Sql}{Limit(componentModel)}";
            return query;
        }

        public static QueryCommandConfig BuildRecordQuery(this ComponentModel componentModel)
        {
            string sql = $"select {AddSelectPart(componentModel, true)} from {componentModel.TableName}";
            QueryCommandConfig query = new QueryCommandConfig(sql);

            componentModel.AddPrimaryKeyFilterPart(query);
            return query;
        }

        public static string AddSearchInputFilterPart(this ComponentModel componentModel, QueryCommandConfig query)
        {
            List<string> filterParts = new List<string>();

            if (!string.IsNullOrEmpty(componentModel.SearchInput))
            {

                foreach (var column in componentModel.GetColumns().Where(c => c.Searchable))
                {
                    ComponentModelExtensions.AddSearchFilterPart(componentModel, column, query, filterParts);
                }

                foreach (var column in componentModel.GetColumns().Where(c => c.Lookup != null && string.IsNullOrEmpty(c.Lookup.TableName) == false))
                {
                    query.Params[$"@{column.ParamName}"] = $"%{componentModel.SearchInput}%";
                    var lookupSql = $"select {column.Lookup.KeyColumn} from {column.Lookup.TableName} where {column.Lookup.DescriptionColumn} like @{column.ParamName}";
                    filterParts.Add($"{RefineSearchExpression(column, componentModel)} in ({lookupSql})");
                }

            }
            return string.Join(" or ", filterParts);
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

        private static void AddPrimaryKeyFilterPart(this ComponentModel componentModel, CommandConfig query)
        {
            var primaryKeyColumn = componentModel.GetColumns().FirstOrDefault(c => c.PrimaryKey);
            query.Sql += $" where {primaryKeyColumn.Expression} = @{primaryKeyColumn.ParamName}";
            query.Params[$"@{primaryKeyColumn.ParamName}"] = ColumnModelHelper.TypedValue(primaryKeyColumn, componentModel.ParentKey) ?? string.Empty;
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

        public static object? ParamValue(object value, ColumnModel column, DataSourceType dataSourceType)
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

        public static bool ParseBoolean(string boolString)
        {
            switch (boolString.ToLower())
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
    }
}