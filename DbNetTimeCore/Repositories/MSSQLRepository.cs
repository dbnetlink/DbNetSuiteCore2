using DbNetTimeCore.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Specialized;
using System.Data;

namespace DbNetTimeCore.Repositories
{
    public class MSSQLRepository : DbRepository, IMSSQLRepository
    {
        public MSSQLRepository(IConfiguration configuration, IWebHostEnvironment env) : base(Enums.DataProvider.SqlClient, configuration, env)
        {
        }
        public async Task<DataTable> GetRecords(GridModel gridModel)
        {
            QueryCommandConfig query = gridModel.BuildQuery();
            return await GetDataTable(query, gridModel.ConnectionAlias);
        }

        public async Task<DataTable> GetColumns(GridModel gridModel)
        {
            QueryCommandConfig query = gridModel.BuildEmptyQuery();
            return await GetDataTable(query, gridModel.ConnectionAlias);
        }


        private CommandConfig BuildUpdate(string fromPart, FormModel formModel, ListDictionary? otherValues = null)
        {
            List<string> columns = formModel.Columns.Where(c => c.IsPrimaryKey == false).Select(c => $"{c.Name} = @{c.Name}").ToList();

            if (otherValues != null)
            {
                columns.AddRange(otherValues.Keys.Cast<string>().Select(c => $"{c} = @{c}").ToList());
            }

            Dictionary<string, object> formValues = new Dictionary<string, object>(); //formModel.FormValues((FormCollection)_httpContextAccessor.HttpContext.Request.Form);

            if (otherValues != null)
            {
                foreach(string columnName in otherValues.Keys)
                {
                    formValues[$"{columnName}"] = otherValues[columnName]; 
                }
            }

            Dictionary<string, object> parameters = formValues.Select(fv => new KeyValuePair<string, object>($"@{fv.Key}", fv.Value)).ToDictionary();

            string sql = $"update {fromPart} set {string.Join(",",columns)}";
            CommandConfig update = new CommandConfig(sql) { Params = parameters };

            AddPrimaryKeyFilterPart(update, formModel);
            return update;
        }

        private void AddPrimaryKeyFilterPart(CommandConfig query, FormModel formModel)
        {
            List<string> filterPart = new List<string>();
            string filterColumn = formModel.Columns.FirstOrDefault(c => c.IsPrimaryKey)!.Name;
            string paramName = filterColumn.Replace(".", string.Empty);
            query.Params[$"@{paramName}"] = formModel.PrimaryKey;
            filterPart.Add($"{filterColumn} = @{paramName}");

            if (filterPart.Any())
            {
                query.Sql += $" where {string.Join(" or ", filterPart)}";
            }
        }


        private async Task BuildLookups(List<EditColumnModel> columns)
        {
            foreach (EditColumnModel column in columns.Where(c => c.Lookup != null))
            {
                column.LookupValues = await GetDataTable(column.Lookup, string.Empty);
            }
        }
    }
}
