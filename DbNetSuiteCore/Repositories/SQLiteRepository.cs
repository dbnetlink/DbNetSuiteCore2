using TQ.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace DbNetTimeCore.Repositories
{
    public class SQLiteRepository : DbRepository, ISQLiteRepository
    {
        public SQLiteRepository(IConfiguration configuration, IWebHostEnvironment env) : base(Enums.DataProvider.SQLite, configuration, env)
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
    }
}
