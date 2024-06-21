using DbNetTimeCore.Enums;
using DbNetTimeCore.Helpers;
using DbNetTimeCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Specialized;
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
        public async Task<DataTable> GetCustomers(GridParameters gridParameters)
        {
            gridParameters.Columns = new List<ColumnInfo>()
            {
                new ColumnInfo("customer.customer_id", "CustomerID") {IsPrimaryKey = true},
                new ColumnInfo("customer.first_name", "Forename", true),
                new ColumnInfo("customer.last_name", "Surname", true),
                new ColumnInfo("customer.email", "Email Address", true) {Format = "email" },
                new ColumnInfo("address.address", "Address", true),
                new ColumnInfo("city.city", "City", true),
                new ColumnInfo("address.postal_code", "Post Code", true),
                new ColumnInfo("customer.active", "Active"),
                new ColumnInfo("customer.create_date", "Created") {Format = "dd/MM/yy", DataType = typeof(DateTime)},
                new ColumnInfo("customer.last_update", "Last Updated") {Format = "dd/MM/yy", DataType = typeof(DateTime)},
            };

            QueryCommandConfig query = BuildQuery("customer join address on customer.address_id = address.address_id join city on city.city_id = address.city_id", gridParameters);
            return await GetDataTable(query);
        }

        public async Task<DataTable> GetCustomer(GridParameters gridParameters)
        {
            gridParameters.Columns = CustomerEditColumns();
            QueryCommandConfig query = BuildQuery("customer", gridParameters);
            return await GetDataTable(query);
        }

        public async Task SaveCustomer(GridParameters gridParameters)
        {
            gridParameters.Columns = CustomerEditColumns();

            CommandConfig update = BuildUpdate("customer", gridParameters, gridParameters.ParameterValues((FormCollection)_httpContextAccessor.HttpContext.Request.Form));
            await ExecuteNonQuery(update);
            gridParameters.Message = "Record updated";
        }

        public async Task<DataTable> GetFilms(GridParameters gridParameters)
        {
            gridParameters.Columns = new List<ColumnInfo>()
            {
                new ColumnInfo("film.film_id", "FilmID") {IsPrimaryKey = true},
                new ColumnInfo("film.title", "Title", true),
                new ColumnInfo("film.description", "Description", true) {MaxTextLength = 40},
                new ColumnInfo("film.release_year", "Year Of Release"),
                new ColumnInfo("language.name", "Language", true),
                new ColumnInfo("film.rental_duration", "Duration"),
                new ColumnInfo("film.rental_rate", "Rental Rate"){Format = "C" },
                new ColumnInfo("film.length", "Length"),
                new ColumnInfo("film.replacement_cost", "Replacement Cost"){Format = "C" },
                new ColumnInfo("film.rating", "Rating"),
                new ColumnInfo("film.special_features", "Special Features", true),
                new ColumnInfo("film.last_update", "Last Updated") {Format = "dd/MM/yy", DataType = typeof(DateTime)},
            };

            QueryCommandConfig query = BuildQuery("film join language on language.language_id = film.language_id", gridParameters);
            return await GetDataTable(query);
        }

        public async Task<DataTable> GetFilm(GridParameters gridParameters)
        {
            gridParameters.Columns = FilmEditColumns();
            QueryCommandConfig query = BuildQuery("film", gridParameters);
            BuildLookups(gridParameters.Columns);
            return await GetDataTable(query);
        }

        public async Task SaveFilm(GridParameters gridParameters)
        {
            gridParameters.Columns = FilmEditColumns();
            CommandConfig update = BuildUpdate("film", gridParameters, gridParameters.ParameterValues((FormCollection)_httpContextAccessor.HttpContext.Request.Form));
            await ExecuteNonQuery(update);
            gridParameters.Message = "Record updated";
        }

        public async Task<DataTable> GetActors(GridParameters gridParameters)
        {
            gridParameters.Columns = new List<ColumnInfo>()
            {
                new ColumnInfo("actor_id", "ActorID") {IsPrimaryKey = true},
                new ColumnInfo("first_name", "Forename", true),
                new ColumnInfo("last_name", "Surname", true),
                new ColumnInfo("last_update", "Last Updated") {Format = "dd/MM/yy", DataType = typeof(DateTime)},
            };

            QueryCommandConfig query = BuildQuery("actor", gridParameters);
            return await GetDataTable(query);
        }

        public async Task<DataTable> GetActor(GridParameters gridParameters)
        {
            gridParameters.Columns = ActorEditColumns();

            QueryCommandConfig query = BuildQuery("actor", gridParameters);
            return await GetDataTable(query);
        }

        public async Task SaveActor(GridParameters gridParameters)
        {
            gridParameters.Columns = ActorEditColumns();
            CommandConfig update = BuildUpdate("actor", gridParameters, gridParameters.ParameterValues((FormCollection)_httpContextAccessor.HttpContext.Request.Form));
            await ExecuteNonQuery(update);
            gridParameters.Message = "Record updated";
        }

        private QueryCommandConfig BuildQuery(string fromPart, GridParameters gridParameters)
        {
            string columns = "*";
            if (gridParameters.Columns.Any())
            {
                columns = string.Join(",", gridParameters.Columns.Select(c => c.Name).ToList());
            }

            string sql = $"select {columns} from {fromPart}";
            QueryCommandConfig query = new QueryCommandConfig(sql);

            AddFilterPart(query, gridParameters);

            if (!string.IsNullOrEmpty(gridParameters.SortKey) || !string.IsNullOrEmpty(gridParameters.CurrentSortKey))
            {
                AddOrderPart(query, gridParameters);
            }

            return query;
        }

        private CommandConfig BuildUpdate(string fromPart, GridParameters gridParameters, ListDictionary values)
        {
            string columns = string.Join(",", gridParameters.Columns.Where(c => c.IsPrimaryKey == false).Select(c => $"{c.Name} = @{c.Name}").ToList());

            string sql = $"update {fromPart} set {columns}";
            CommandConfig update = new CommandConfig(sql) { Params = values };

            foreach (string name in values.Keys)
            {
            }

            AddFilterPart(update, gridParameters);
            return update;
        }

        private void AddFilterPart(CommandConfig query, GridParameters gridParameters)
        {
            List<string> filterPart = new List<string>();

            switch (gridParameters.Handler)
            {
                case "edit":
                case "save":
                    string filterColumn = gridParameters.Columns.FirstOrDefault(c => c.IsPrimaryKey)!.Name;
                    string paramName = filterColumn.Replace(".", string.Empty);
                    query.Params[$"@{paramName}"] = gridParameters.PrimaryKey;
                    filterPart.Add($"{filterColumn} = @{paramName}");
                    break;
                default:
                    foreach (var col in gridParameters.Columns.Where(c => c.Searchable).Select(c => c.Name).ToList())
                    {
                        paramName = col.Replace(".", string.Empty);
                        query.Params[$"@{paramName}"] = $"%{gridParameters.SearchInput}%";
                        filterPart.Add($"{col} like @{paramName}");
                    }
                    break;
            }

            if (filterPart.Any())
            {
                query.Sql += $" where {string.Join(" or ", filterPart)}";
            }
        }

        private void AddOrderPart(QueryCommandConfig query, GridParameters gridParameters)
        {
            query.Sql += $" order by {(!string.IsNullOrEmpty(gridParameters.SortKey) ? gridParameters.SortColumn : gridParameters.CurrentSortColumn)} {gridParameters.SortSequence}";
        }

        private List<ColumnInfo> CustomerEditColumns()
        {
            return new List<ColumnInfo>()
            {
                new ColumnInfo("customer_id", "CustomerID") {IsPrimaryKey = true},
                new ColumnInfo("first_name", "Forename", true),
                new ColumnInfo("last_name", "Surname", true),
                new ColumnInfo("email", "Email Address", true) {Format = "email", ClassName = "w-80" },
                new ColumnInfo("active", "Active") {DataType = typeof(Boolean)}
            };
        }

        private List<ColumnInfo> FilmEditColumns()
        {
            return new List<ColumnInfo>()
            {
                new ColumnInfo("film_id", "FilmID") {IsPrimaryKey = true},
                new ColumnInfo("title", "Title", true),
                new ColumnInfo("description", "Description", true){EditControlType = EditControlType.TextArea },
                new ColumnInfo("release_year", "Year Of Release"),
                new ColumnInfo("language_id", "Language", true) { Lookup = new QueryCommandConfig("select language_id, name from language order by 2")},
                new ColumnInfo("rental_duration", "Duration"),
                new ColumnInfo("rental_rate", "Rental Rate"),
                new ColumnInfo("length", "Length"),
                new ColumnInfo("replacement_cost", "Replacement Cost"),
                new ColumnInfo("rating", "Rating") {LookupEnum = typeof(FilmRating)},
                new ColumnInfo("special_features", "Special Features", true) {EditControlType = EditControlType.MultiSelect, LookupEnum = typeof(SpecialFeature)},
            };
        }

        private List<ColumnInfo> ActorEditColumns()
        {
            return new List<ColumnInfo>()
            {
                new ColumnInfo("actor_id", "ActorID") {IsPrimaryKey = true},
                new ColumnInfo("first_name", "Forename", true),
                new ColumnInfo("last_name", "Surname", true)
            };
        }

        private async Task BuildLookups(List<ColumnInfo> columns)
        {
            foreach (ColumnInfo column in columns.Where(c => c.Lookup != null))
            {
                column.LookupValues = await GetDataTable(column.Lookup);
            }
        }
    }
}
