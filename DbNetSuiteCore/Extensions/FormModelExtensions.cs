using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;

namespace DbNetSuiteCore.Extensions
{
    public static class FormModelExtensions
    {
        public static void AddFilterPart(this FormModel formModel, QueryCommandConfig query)
        {
            List<string> filterParts = new List<string>();

            var filter = ComponentModelExtensions.AddSearchInputFilterPart(formModel, query);

            if (string.IsNullOrEmpty(filter) == false)
            {
                filterParts.Add(filter);
            }

            if (!string.IsNullOrEmpty(formModel.FixedFilter))
            {
                filterParts.Add($"({formModel.FixedFilter})");
                ComponentModelExtensions.AssignParameters(query, formModel.FixedFilterParameters);
            }

            if (filterParts.Any())
            {
                query.Sql += $" where {string.Join(" and ", filterParts)}";
            }
        }

        public static CommandConfig BuildUpdate(this FormModel formModel)
        {
            CommandConfig update = new CommandConfig();

            update.Sql = $"update {formModel.TableName}";

            List<string> set = new List<string>();

            foreach (string columnName in formModel.FormValues.Keys)
            {
                FormColumn? formColumn = formModel.Columns.First(c => c.ColumnName == columnName);

                if (formColumn == null)
                {
                    continue;
                }
                var paramName = DbHelper.ParameterName(columnName);
                var value  = formModel.FormValues[columnName];

                set.Add($"{columnName} = {paramName}");
                update.Params[paramName] = string.IsNullOrEmpty(value) ? DBNull.Value : (ComponentModelExtensions.ParamValue(value, formColumn, formModel.DataSourceType) ?? DBNull.Value);
            }

            if (set.Any())
            {
                update.Sql += $" set {string.Join(",", set)}";
            }
            var primaryKeyColumn = formModel.Columns.First(c => c.PrimaryKey);
            update.Sql += $" where {primaryKeyColumn.ColumnName} = {DbHelper.ParameterName(primaryKeyColumn.ColumnName)}";
            update.Params[primaryKeyColumn.ColumnName] = ComponentModelExtensions.ParamValue(formModel.ParentKey, primaryKeyColumn, formModel.DataSourceType) ?? DBNull.Value;

            return update;
        }
    }
}