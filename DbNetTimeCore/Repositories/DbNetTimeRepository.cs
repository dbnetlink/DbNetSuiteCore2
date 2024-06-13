using DbNetTimeCore.Models;
using System.Data;
using static DbNetTimeCore.Utilities.DbNetDataCore;

namespace DbNetTimeCore.Repositories
{
    public class DbNetTimeRepository : DbRepository, IDbNetTimeRepository
    {
        public DbNetTimeRepository(IConfiguration configuration, IWebHostEnvironment env) : base(configuration, env)
        {
        }
        public DataTable GetCustomers(GridParameters gridParameters)
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
            return GetDataTable(query);
        }

        public DataTable GetCustomer(GridParameters gridParameters)
        {
            gridParameters.Columns = new List<ColumnInfo>()
            {
                new ColumnInfo("customer.customer_id", "CustomerID") {IsPrimaryKey = true},
                new ColumnInfo("customer.first_name", "Forename", true),
                new ColumnInfo("customer.last_name", "Surname", true),
                new ColumnInfo("customer.email", "Email Address", true) {Format = "email" },
                new ColumnInfo("customer.active", "Active"),
                new ColumnInfo("customer.create_date", "Created") {Format = "dd/MM/yy", DataType = typeof(DateTime)},
                new ColumnInfo("customer.last_update", "Last Updated") {Format = "dd/MM/yy", DataType = typeof(DateTime)},
            };

            QueryCommandConfig query = BuildQuery("customer", gridParameters);
            return GetDataTable(query);
        }

        public DataTable GetFilms(GridParameters gridParameters)
        {
            gridParameters.Columns = new List<ColumnInfo>()
            {
                new ColumnInfo("film.film_id", "FilmID") {IsPrimaryKey = true},
                new ColumnInfo("film.title", "Title", true),
                new ColumnInfo("film.description", "Description", true),
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
            return GetDataTable(query);
        }

        public DataTable GetActors(GridParameters gridParameters)
        {
            gridParameters.Columns = new List<ColumnInfo>()
            {
                new ColumnInfo("actor_id", "ActorID") {IsPrimaryKey = true},
                new ColumnInfo("first_name", "Forename", true),
                new ColumnInfo("last_name", "Surname", true),
                new ColumnInfo("last_update", "Last Updated") {Format = "dd/MM/yy", DataType = typeof(DateTime)},
            };

            QueryCommandConfig query = BuildQuery("actor", gridParameters);
            return GetDataTable(query);
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

        private void AddFilterPart(QueryCommandConfig query, GridParameters gridParameters)
        {
            List<string> filterPart = new List<string>();

            if (gridParameters.Handler == "edit")
            {
                string filterColumn = gridParameters.Columns.FirstOrDefault(c => c.IsPrimaryKey)!.Name;
                string paramName = filterColumn.Replace(".", string.Empty);
                query.Params[$"@{paramName}"] = gridParameters.PrimaryKey;
                filterPart.Add($"{filterColumn} = @{paramName}");
            }
            else
            {
                foreach (string filterColumn in gridParameters.Columns.Where(c => c.Searchable).Select(c => c.Name).ToList())
                {
                    string paramName = filterColumn.Replace(".", string.Empty);
                    query.Params[$"@{paramName}"] = $"%{gridParameters.SearchInput}%";
                    filterPart.Add($"{filterColumn} like @{paramName}");
                }
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
    }
}
