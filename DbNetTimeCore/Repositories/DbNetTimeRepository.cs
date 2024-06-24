using DbNetTimeCore.Helpers;
using DbNetTimeCore.Models;
using System.Data;
using static DbNetTimeCore.Utilities.DbNetDataCore;

namespace DbNetTimeCore.Repositories
{
    public class DbNetTimeRepository : DbRepository, IDbNetTimeRepository
    {
        IHttpContextAccessor _httpContextAccessor;
        public DbNetTimeRepository(IConfiguration configuration, IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor) : base(configuration, env)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<DataTable> GetCustomers(GridModel gridModel)
        {
            QueryCommandConfig query = BuildQuery("customer join address on customer.address_id = address.address_id join city on city.city_id = address.city_id", gridModel);
            return await GetDataTable(query);
        }

        public async Task<DataTable> GetCustomer(FormModel formModel)
        {
            QueryCommandConfig query = BuildQuery("customer", formModel);
            return await GetDataTable(query);
        }

        public async Task SaveCustomer(FormModel formModel)
        {
            CommandConfig update = BuildUpdate("customer", formModel);
            await ExecuteNonQuery(update);
            formModel.Message = "Record updated";
        }

        public async Task<DataTable> GetFilms(GridModel gridModel)
        {
            QueryCommandConfig query = BuildQuery("film join language on language.language_id = film.language_id", gridModel);
            return await GetDataTable(query);
        }

        public async Task<DataTable> GetFilm(FormModel formModel)
        {
            QueryCommandConfig query = BuildQuery("film", formModel);
            BuildLookups(formModel.EditColumns);
            return await GetDataTable(query);
        }

        public async Task SaveFilm(FormModel formModel)
        {
            CommandConfig update = BuildUpdate("film", formModel);
            await ExecuteNonQuery(update);
            formModel.Message = "Record updated";
        }

        public async Task<DataTable> GetActors(GridModel gridModel)
        {
            QueryCommandConfig query = BuildQuery("actor", gridModel);
            return await GetDataTable(query);
        }

        public async Task<DataTable> GetActor(FormModel formModel)
        {
            QueryCommandConfig query = BuildQuery("actor", formModel);
            return await GetDataTable(query);
        }

        public async Task SaveActor(FormModel formModel)
        {
            CommandConfig update = BuildUpdate("actor", formModel);
            await ExecuteNonQuery(update);
            formModel.Message = "Record updated";
        }

        private QueryCommandConfig BuildQuery(string fromPart, ComponentModel componentModel)
        {
            string columns = "*";
            if (componentModel.Columns.Any())
            {
                columns = string.Join(",", componentModel.Columns.Select(c => c.Name).ToList());
            }
          
            string sql = $"select {columns} from {fromPart}";
            QueryCommandConfig query = new QueryCommandConfig(sql);

            AddFilterPart(query, componentModel);

            if (componentModel is GridModel)
            {
                var gridModel = (GridModel)componentModel;
               
                if (!string.IsNullOrEmpty(gridModel.SortKey) || !string.IsNullOrEmpty(gridModel.CurrentSortKey))
                {
                    AddOrderPart(query, gridModel);
                }
            }

            return query;
        }

        private CommandConfig BuildUpdate(string fromPart, FormModel formModel)
        {
            string columns = string.Join(",", formModel.Columns.Where(c => c.IsPrimaryKey == false).Select(c => $"{c.Name} = @{c.Name}").ToList());

            var formValues = formModel.FormValues((FormCollection)_httpContextAccessor.HttpContext.Request.Form);

            string sql = $"update {fromPart} set {columns}";
            CommandConfig update = new CommandConfig(sql) { Params = formValues };

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

        private void AddFilterPart(CommandConfig query, ComponentModel componentModel)
        {
            if (componentModel is FormModel)
            {
                AddPrimaryKeyFilterPart(query,(FormModel)componentModel);
                return;
            }

            var gridModel = (GridModel)componentModel;

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
                column.LookupValues = await GetDataTable(column.Lookup);
            }
        }
    }
}
