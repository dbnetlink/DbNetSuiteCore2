using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using System.Text.RegularExpressions;
using System.Data;
using System.Globalization;
using DbNetSuiteCore.Attributes;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;

namespace DbNetSuiteCore.Extensions
{
    public static class ComponentModelExtensions
    {
        public static QueryCommandConfig BuildEmptyQuery(this ComponentModel componentModel)
        {
            return new QueryCommandConfig(componentModel.DataSourceType) { Sql = $"select {ColumnsHelper.GetColumnExpressions(componentModel.GetColumns())} from {componentModel.TableName} where 1=2" };
        }

        public static QueryCommandConfig BuildQuery(this ComponentModel componentModel)
        {
            string sql = $"select {Distinct(componentModel)}{Top(componentModel)}{AddSelectPart(componentModel)} from {componentModel.TableName}";
            QueryCommandConfig query = new QueryCommandConfig(componentModel.DataSourceType) { Sql = sql };

            if (componentModel is GridModel)
            {
                var gridModel = (GridModel)componentModel;
                gridModel.AddFilterPart(query);
                if (gridModel.IsGrouped)
                {
                    gridModel.AddGroupByPart(query);
                    gridModel.AddHavingPart(query);
                }
                gridModel.AddOrderPart(query);
                gridModel.AddPagination(query);
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

        public static QueryCommandConfig BuildCountQuery(this ComponentModel componentModel)
        {
            QueryCommandConfig query = new QueryCommandConfig(componentModel.DataSourceType) { Sql = $"select count(*) from {componentModel.TableName}" };
            var gridModel = (GridModel)componentModel;
            gridModel.AddFilterPart(query);
            return query;
        }

        public static QueryCommandConfig BuildDistinctQuery(this ComponentModel componentModel, ColumnModel column)
        {
            List<string> columns = new List<string>()
            {
                DbHelper.QualifyExpression(column.Expression, componentModel.DataSourceType),
                DbHelper.QualifyExpression(column.Expression, componentModel.DataSourceType)
            };
            QueryCommandConfig query = new QueryCommandConfig(componentModel.DataSourceType) { Sql = $"select distinct {string.Join(",", columns)} from {componentModel.TableName}" };
            var gridModel = (GridModel)componentModel;
            gridModel.AddFilterPart(query);
            query.Sql += " order by 1";
            return query;
        }

        public static QueryCommandConfig BuildSubSelectQuery(this ComponentModel componentModel, ColumnModel column)
        {
            QueryCommandConfig query = new QueryCommandConfig(componentModel.DataSourceType) { Sql = $"select distinct {DbHelper.QualifyExpression(column.Expression, componentModel.DataSourceType)} from {componentModel.TableName}" };
            var gridModel = (GridModel)componentModel;
            gridModel.AddFilterPart(query);
            return query;
        }

        public static QueryCommandConfig BuildRecordQuery(this ComponentModel componentModel, object? primaryKeyValue = null)
        {
            string sql = $"select {AddSelectPart(componentModel, true)} from {componentModel.TableName}";
            QueryCommandConfig query = new QueryCommandConfig(componentModel.DataSourceType) { Sql = sql };
            AddPrimaryKeyFilterPart(componentModel, query, primaryKeyValue /*== null ? componentModel.ParentKey*/ : primaryKeyValue);
            return query;
        }

        public static QueryCommandConfig BuildUniqueQuery(this FormModel formModel, GridFormColumn column)
        {
            string sql = $"select count(*) from {formModel.TableName}";
            QueryCommandConfig query = new QueryCommandConfig(formModel.DataSourceType) { Sql = sql };

            if (formModel.Mode == FormMode.Update)
            {
                AddPrimaryKeyFilterPart(formModel, query, formModel.RecordId ?? new List<object>(), "<>");
            }
            query.Sql += $" {(formModel.Mode == FormMode.Update ? "and" : "where")} {column.Expression} = @{column.Name}";

            if (formModel.Columns.Any(c => c.ForeignKey))
            {
                var foreignKeyColumn = formModel.Columns.First(c => c.ForeignKey);
                if (formModel.Mode == FormMode.Insert && foreignKeyColumn.InitialValue != null)
                {
                    query.Sql += $" and {foreignKeyColumn.Expression} = @{foreignKeyColumn.Name}";
                    query.Params[foreignKeyColumn.Name] = foreignKeyColumn.InitialValue;
                }
            }
            query.Params[column.Name] = formModel.FormValues[column.Name];

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
                    var paramName = DbHelper.ParameterName(column.ParamName, componentModel.DataSourceType);
                    query.Params[$"{paramName}"] = $"%{componentModel.SearchInput}%";
                    var lookupSql = $"select {column.Lookup?.KeyColumn} from {column.Lookup?.TableName} where {column.Lookup?.DescriptionColumn} like {paramName}";
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
                    foreach (string paramValue in searchDialogFilter.Value1.Split(","))
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
                    foreach (int i in Enumerable.Range(0, 2))
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
                return DbHelper.ParameterName($"sd_{columnKey}{idx}", componentModel.DataSourceType);
            }
        }

        public static object? SearchFilterParam(SearchOperator searchOperator, object? value)
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

        private static void AddPrimaryKeyFilterPart(ComponentModel componentModel, CommandConfig query, object primaryKeyValue, string oper = "=")
        {
            if (primaryKeyValue is not List<object>)
            {
                primaryKeyValue = new List<object>() { primaryKeyValue };
            }

            List<string> where = new List<string>();
            foreach (var item in componentModel.GetColumns().Where(c => c.PrimaryKey).Select((value, index) => new { value, index }))
            {
                var paramName = DbHelper.ParameterName(item.value.ParamName, componentModel.DataSourceType);
                where.Add($"{item.value.Expression} {oper} {paramName}");
                query.Params[$"{paramName}"] = ColumnModelHelper.TypedValue(item.value, (primaryKeyValue as List<object>)[item.index]) ?? string.Empty;
            }
            query.Sql += $" where ({string.Join(" and ", where)})";
        }

        public static void AddParentKeyFilterPart(ComponentModel componentModel, CommandConfig query, List<string> filterParts)
        {
            if (componentModel.ParentModel.RowIdx < 0)
            {
                filterParts.Add($"(1=2)");
                return;
            }

            List<object> parentKeyValues = componentModel.GetParentKeyValues();

            IEnumerable<ColumnModel> keyColumns = ((componentModel as FormModel)?.OneToOne ?? false) ? componentModel.GetColumns().Where(c => c.PrimaryKey) : componentModel.GetColumns().Where(c => c.ForeignKey);

            IEnumerable<ColumnModel> foreignKeyColumns = ((componentModel as FormModel)?.OneToOne ?? false) ? new List<ColumnModel>() : componentModel.GetColumns().Where(c => c.ForeignKey);

            foreach (var item in foreignKeyColumns.Select((value, index) => new { value = value, index = index }))
            {
                if (string.IsNullOrEmpty(item.value.ForeignKeyParentColumn) == false)
                {
                    parentKeyValues[item.index] = componentModel.ParentModel!.ParentRow[item.value.ForeignKeyParentColumn];
                }
            }

            foreach (var item in keyColumns.Select((value, index) => new { value = value, index = index }))
            {
                var paramName = DbHelper.ParameterName(item.value.ParamName, componentModel.DataSourceType);
                filterParts.Add($"({DbHelper.StripColumnRename(item.value.Expression)} = {paramName})");
                query.Params[$"{paramName}"] = ColumnModelHelper.TypedValue(item.value, parentKeyValues[item.index]) ?? string.Empty;
            }
        }

        public static QueryCommandConfig BuildProcedureCall(this ComponentModel componentModel)
        {
            QueryCommandConfig query = new QueryCommandConfig(componentModel.DataSourceType) { Sql = $"{componentModel.ProcedureName}" };
            AssignParameters(query, componentModel.ProcedureParameters);
            return query;
        }

        public static void AssignParameters(QueryCommandConfig query, List<DbParameter> parameters)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.Value is System.Text.Json.JsonElement)
                {
                    parameter.Value = JsonElementExtension.Value((System.Text.Json.JsonElement)parameter.Value);
                }
                query.Params[DbHelper.ParameterName(parameter.Name, query.DataSourceType)] = ColumnModelHelper.TypedValue(parameter.TypeName, parameter.Value);
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
            query.Params[$"{DbHelper.ParameterName(columnModel.ParamName, query.DataSourceType)}"] = $"%{searchInput}%";
            filterParts.Add($"{expression} like {DbHelper.ParameterName(columnModel.ParamName, query.DataSourceType)}");
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
                case DataSourceType.Oracle:
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

            if (componentModel is GridModel gridModel)
            {
                foreach (var column in gridModel.Columns.Where(c => string.IsNullOrEmpty(c.ParseFormat) == false))
                {
                    DataColumn? dataColumn = componentModel.GetDataColumn(column);
                    componentModel.Data.ParseColumnDataType(dataColumn, column, gridModel);
                }
            }
        }

        public static bool IsCsvFile(ComponentModel componentModel)
        {
            return componentModel.DataSourceType == DataSourceType.Excel && componentModel.Url.ToLower().Replace("]", string.Empty).EndsWith(".csv");
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

        public static void AssignParentModel(this ComponentModel componentModel, HttpContext context, IConfiguration configuration, string key = "parentModel")
        {
            string parentModel = RequestHelper.FormValue(key, string.Empty, context);
            if (string.IsNullOrEmpty(parentModel) == false)
            {
                string json = TextHelper.DeobfuscateString(parentModel, configuration, context);
                if (string.IsNullOrEmpty(json) == false)
                {
                    componentModel.ParentModel = JsonConvert.DeserializeObject<SummaryModel>(json);

                    string rowIndex = RequestHelper.FormValue("rowindex", string.Empty, context);

                    if (string.IsNullOrEmpty(rowIndex) == false)
                    {
                        componentModel.ParentModel.RowIdx = int.Parse(rowIndex);
                    }
                }
            }
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
                if (dataType == nameof(Boolean))
                {
                    return false;
                }
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

        public static int ParseInt(string intString)
        {
            int value = -1;
            Int32.TryParse(intString, out value);
            return value;
        }
        private static Type GetColumnType(string typeName)
        {
            return Type.GetType("System." + typeName);
        }

        public static string Limit(ComponentModel componentModel)
        {
            if (componentModel is GridModel)
            {
                if (((GridModel)componentModel).OptimizeForLargeDataset)
                {
                    return string.Empty;
                }
            }
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