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

        public static CommandConfig BuildUpdate(this FormModel formModel)
        {
            CommandConfig update = new CommandConfig();
            update.Sql = $"update {formModel.TableName}";

            List<string> set = new List<string>();

            foreach (FormColumn formColumn in formModel.Columns.Where(c => c.PrimaryKey == false))
            {
                if (formColumn.ReadOnly || formColumn.Disabled)
                {
                    continue;
                };
               
                if (formModel.FormValues.Keys.Contains(formColumn.ColumnName))
                {
                    var columnName = formColumn.ColumnName;
                    var paramName = DbHelper.ParameterName(columnName);
                    set.Add($"{columnName} = {paramName}");
                    update.Params[paramName] = GetParamValue(formModel, formColumn);
                }
            }

            if (set.Any())
            {
                update.Sql += $" set {string.Join(",", set)}";
            }
            var primaryKeyColumn = formModel.Columns.First(c => c.PrimaryKey);
            update.Sql += $" where {primaryKeyColumn.ColumnName} = {DbHelper.ParameterName(primaryKeyColumn.ColumnName)}";
            update.Params[primaryKeyColumn.ColumnName] = ComponentModelExtensions.ParamValue(formModel.RecordId, primaryKeyColumn, formModel.DataSourceType) ?? DBNull.Value;

            return update;
        }

        public static CommandConfig BuildInsert(this FormModel formModel)
        {
            CommandConfig insert = new CommandConfig();

            List<string> columnNames = new List<string>();
            List<string> paramNames = new List<string>();

            foreach (FormColumn formColumn in formModel.Columns.Where(c => c.Autoincrement == false))
            {
                if (formColumn.ReadOnly || formColumn.Disabled)
                {
                    continue;
                };
  
                if (formModel.FormValues.Keys.Contains(formColumn.ColumnName))
                {
                    var paramName = DbHelper.ParameterName(formColumn.ColumnName);
                    columnNames.Add(formColumn.ColumnName);
                    paramNames.Add(paramName);
                    insert.Params[paramName] = GetParamValue(formModel, formColumn);
                }
            }

            insert.Sql = $"insert into {formModel.TableName} ({string.Join(",",columnNames)}) values ({string.Join(",", paramNames)})";
            return insert;
        }


        private static object GetParamValue(FormModel formModel, FormColumn formColumn)
        {
            string columnName = formColumn.ColumnName;
            string value = string.Empty;
            if (formModel.FormValues.Keys.Contains(columnName))
            {
                value = formModel.FormValues[columnName];
            }
            else if (formColumn.DataType != typeof(bool))
            {
                throw new Exception($"Form data missing for column => <b>{columnName}</b>");
            }
            else
            {
                value = "false";
            }
            return string.IsNullOrEmpty(value) ? DBNull.Value : (ComponentModelExtensions.ParamValue(value, formColumn, formModel.DataSourceType) ?? DBNull.Value);
        }

        public static CommandConfig BuildDelete(this FormModel formModel)
        {
            CommandConfig delete = new CommandConfig();
            var primaryKeyColumn = formModel.Columns.First(c => c.PrimaryKey);
            delete.Sql = $"delete from {formModel.TableName} where { primaryKeyColumn.ColumnName} = { DbHelper.ParameterName(primaryKeyColumn.ColumnName)}";
            delete.Params[primaryKeyColumn.ColumnName] = ComponentModelExtensions.ParamValue(formModel.RecordId, primaryKeyColumn, formModel.DataSourceType) ?? DBNull.Value;
            return delete;
        }
    }
}