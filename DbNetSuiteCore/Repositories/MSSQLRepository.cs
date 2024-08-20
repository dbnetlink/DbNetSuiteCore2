using TQ.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Specialized;
using System.Data;

namespace DbNetSuiteCore.Repositories
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
            return await GetSchemaTable(query, gridModel.ConnectionAlias);
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
