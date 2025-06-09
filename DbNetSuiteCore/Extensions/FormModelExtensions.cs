using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using System.Reflection.Metadata.Ecma335;

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

            string searchDialogFilter = ComponentModelExtensions.AddSearchDialogFilterPart(formModel, query);
            if (string.IsNullOrEmpty(searchDialogFilter) == false)
            {
                filterParts.Add(searchDialogFilter);
            }

            if (formModel.IsLinked)
            {
                ComponentModelExtensions.AddParentKeyFilterPart(formModel, query, filterParts);
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

        public static void AddOrderPart(this FormModel formModel, QueryCommandConfig query)
        {
            string columnName = "1";
            var sequence = "asc";

            var initialSortColumn = formModel.Columns.FirstOrDefault(c => c.InitialSortOrder.HasValue);

            if (initialSortColumn != null)
            {
                columnName = initialSortColumn.ColumnName;
                sequence = initialSortColumn.InitialSortOrder.ToString()?.ToLower();
            }

            query.Sql += $" order by {columnName} {sequence}";
        }

        public static CommandConfig BuildUpdate(this FormModel formModel)
        {
            CommandConfig update = new CommandConfig(formModel.DataSourceType);
            update.Sql = $"update {formModel.TableName}";

            List<string> set = new List<string>();

            foreach (FormColumn formColumn in formModel.Columns.Where(c => c.PrimaryKey == false))
            {
                if (formColumn.IsReadOnly(formModel.Mode) || formColumn.Disabled)
                {
                    continue;
                }

                if (formModel.Modified.Columns.Contains(formColumn.ColumnName, StringComparer.CurrentCultureIgnoreCase) == false)
                {
                    continue;
                }

                if (formModel.FormValues.Keys.Contains(formColumn.ColumnName, StringComparer.CurrentCultureIgnoreCase))
                {
                    var columnName = formColumn.ColumnName;
                    var paramName = DbHelper.ParameterName(columnName, formModel.DataSourceType);
                    update.Params[paramName] = GetParamValue(formModel, formColumn);
                    paramName = ComponentModelExtensions.UpdateParamName(paramName, formColumn, formModel.DataSourceType);

                    set.Add($"{columnName} = {paramName}");
                }
            }

            if (set.Any())
            {
                update.Sql += $" set {string.Join(",", set)}";
            }

            formModel.AddWhereClause(update);

            return update;
        }

        private static void AddWhereClause(this FormModel formModel, CommandConfig commandConfig)
        {
            List<string> where = new List<string>();

            var recordId = formModel.RecordId as List<object> ?? new List<object>();

            foreach (var item in formModel.Columns.Where(c => c.PrimaryKey).Select((value, index) => new { value = value, index = index }))
            {
                where.Add($"{item.value.ColumnName} = {DbHelper.ParameterName(item.value.ColumnName, formModel.DataSourceType)}");
                commandConfig.Params[item.value.ColumnName] = ComponentModelExtensions.ParamValue(recordId[item.index], item.value, formModel.DataSourceType) ?? DBNull.Value;
            }

            commandConfig.Sql += $" where {string.Join(" and ", where)}";
        }

        public static CommandConfig BuildInsert(this FormModel formModel)
        {
            CommandConfig insert = new CommandConfig(formModel.DataSourceType);

            List<string> columnNames = new List<string>();
            List<string> paramNames = new List<string>();

            foreach (FormColumn formColumn in formModel.Columns.Where(c => c.Autoincrement == false || string.IsNullOrEmpty(c.SequenceName) == false))
            {
                if ((formColumn.IsReadOnly(formModel.Mode) || formColumn.Disabled) && string.IsNullOrEmpty(formColumn.SequenceName))
                {
                    continue;
                }
                ;

                if (formModel.FormValues.Keys.Contains(formColumn.ColumnName))
                {
                    var paramName = DbHelper.ParameterName(formColumn.ColumnName, formModel.DataSourceType);
                    insert.Params[paramName] = GetParamValue(formModel, formColumn);
                    columnNames.Add(formColumn.ColumnName);
                    paramName = ComponentModelExtensions.UpdateParamName(paramName, formColumn, formModel.DataSourceType);
                    paramNames.Add(paramName);

                }
            }

            insert.Sql = $"insert into {formModel.TableName} ({string.Join(",", columnNames)}) values ({string.Join(",", paramNames)})";
            return insert;
        }

        public static object GetParamValue(FormModel formModel, FormColumn formColumn)
        {
            string columnName = formColumn.ColumnName;
            string value = string.Empty;
            if (formModel.FormValues.Keys.Contains(columnName))
            {
                value = formModel.FormValues[columnName];

                if (formColumn.HashPassword)
                {
                    return PasswordHash.Hash(value);
                }
            }
            else if (formColumn.DataType != typeof(bool))
            {
                throw new Exception($"Form data missing for column => <b>{columnName}</b>");
            }
            else
            {
                value = "false";
            }
            return ComponentModelExtensions.ParamValue(value, formColumn, formModel.DataSourceType) ?? DBNull.Value;
        }

        public static CommandConfig BuildDelete(this FormModel formModel)
        {
            CommandConfig delete = new CommandConfig(formModel.DataSourceType);
            delete.Sql = $"delete from {formModel.TableName}";
            formModel.AddWhereClause(delete);
            return delete;
        }
    }
}