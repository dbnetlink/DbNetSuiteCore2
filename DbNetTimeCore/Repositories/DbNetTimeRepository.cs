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
            QueryCommandConfig query = new  QueryCommandConfig("select * from customer");

            if (!string.IsNullOrEmpty(gridParameters.SearchInput))
            {
                AddFilterPart(query, new List<string>() { "first_name", "last_name", "email" }, gridParameters.SearchInput);

            }
            return GetDataTable(query);
        }

        public DataTable GetFilms(GridParameters gridParameters)
        {
            QueryCommandConfig query = new QueryCommandConfig("select * from film");

            if (!string.IsNullOrEmpty(gridParameters.SearchInput))
            {
                AddFilterPart(query, new List<string>() { "first_name", "last_name", "email" }, gridParameters.SearchInput);

            }
            return GetDataTable(query);
        }

        public DataTable GetActors(GridParameters gridParameters)
        {
            QueryCommandConfig query = new QueryCommandConfig("select * from actor");

            if (!string.IsNullOrEmpty(gridParameters.SearchInput))
            {
                AddFilterPart(query, new List<string>() { "first_name", "last_name", "email" }, gridParameters.SearchInput);

            }
            return GetDataTable(query);
        }

        private void AddFilterPart(QueryCommandConfig query, List<string> filterColumns, string filterValue)
        {
            List<string> filterPart = new List<string>();

            foreach (string filterColumn in filterColumns)
            {
                query.Params[filterColumn] = $"%{filterValue}%";
                filterPart.Add($"{filterColumn} = @{filterColumn}");
            }

            query.Sql += $" where {string.Join(" or ", filterPart)}";
        }
    }
}
