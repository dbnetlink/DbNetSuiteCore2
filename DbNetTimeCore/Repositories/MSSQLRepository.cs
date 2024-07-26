using DbNetTimeCore.Models;
using System.Collections.Specialized;
using System.Data;
using static DbNetTimeCore.Utilities.DbNetDataCore;

namespace DbNetTimeCore.Repositories
{
    public class MSSQLRepository : DbRepository, IMSSQLRepository
    {
        IHttpContextAccessor _httpContextAccessor;
        public MSSQLRepository(IConfiguration configuration, IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor) : base(configuration, env)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<DataTable> GetRecords(GridModel gridModel)
        {
            QueryCommandConfig query = BuildQuery(gridModel);
            return await GetDataTable(query, gridModel.ConnectionAlias);
        }

        public async Task<DataTable> GetColumns(GridModel gridModel)
        {
            QueryCommandConfig query = new QueryCommandConfig($"select * from {gridModel.TableName} where 1=2");
            return await GetDataTable(query, gridModel.ConnectionAlias);
        }

        private QueryCommandConfig BuildQuery(GridModel gridModel)
        {
            string columns = "*";
            if (gridModel.Columns.Any())
            {
                columns = string.Join(",", gridModel.Columns.Select(c => c.Name).ToList());
            }
          
            string sql = $"select {columns} from {gridModel.TableName}";
            QueryCommandConfig query = new QueryCommandConfig(sql);

            AddFilterPart(query, gridModel);

            if (gridModel is GridModel)
            {
                if (!string.IsNullOrEmpty(gridModel.SortKey) || !string.IsNullOrEmpty(gridModel.CurrentSortKey))
                {
                    AddOrderPart(query, gridModel);
                }
            }

            return query;
        }

        private CommandConfig BuildUpdate(string fromPart, FormModel formModel, ListDictionary? otherValues = null)
        {
            List<string> columns = formModel.Columns.Where(c => c.IsPrimaryKey == false).Select(c => $"{c.Name} = @{c.Name}").ToList();

            if (otherValues != null)
            {
                columns.AddRange(otherValues.Keys.Cast<string>().Select(c => $"{c} = @{c}").ToList());
            }

            Dictionary<string,object> formValues = formModel.FormValues((FormCollection)_httpContextAccessor.HttpContext.Request.Form);

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

        private void AddFilterPart(CommandConfig query, GridModel gridModel)
        {
            List<string> filterPart = new List<string>();

            foreach (var col in gridModel.GridColumns.Where(c => c.Searchable).Select(c => c.Name).ToList())
            {
                string paramName = col.Replace(".", string.Empty);
                query.Params[$"@{paramName}"] = $"%{gridModel.SearchInput}%";
                filterPart.Add($"{col} like @{paramName}");
            }

            if (filterPart.Any())
            {
                query.Sql += $" where {string.Join(" or ", filterPart)}";
            }
        }

        private void AddOrderPart(QueryCommandConfig query, GridModel gridModel)
        {
            query.Sql += $" order by {(!string.IsNullOrEmpty(gridModel.SortKey) ? gridModel.SortColumn : gridModel.CurrentSortColumn)} {gridModel.SortSequence}";
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
