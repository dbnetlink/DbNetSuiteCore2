using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Data;


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
            string sql = $"select {Distinct(componentModel)}{Top(componentModel)}{ComponentModelExtensions.AddSelectPart(componentModel)} from {componentModel.TableName}";
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

            query.Sql = $"{query.Sql}{Limit(componentModel)}";
            return query;
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
                query.Params[DbHelper.ParameterName(parameter.Name)] = GridColumnModelExtensions.TypedValue(parameter.TypeName, parameter.Value);
            }
        }

        public static string AddSelectPart(this ComponentModel componentModel)
        {
            if (componentModel.GetColumns().Any() == false)
            {
                return "*";
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
            string searchInput = componentModel.SearchInput;
            string expression = DbHelper.StripColumnRename(columnModel.Expression);

            if (IsCsvFile(componentModel))
            {
                searchInput = searchInput.ToLower();
                expression = $"LCASE({expression})";
            }

            query.Params[$"@{columnModel.ParamName}"] = $"%{searchInput}%";
            filterParts.Add($"{expression} like @{columnModel.ParamName}");
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