using DbNetTimeCore.Models;
using System.Data;

namespace DbNetTimeCore.Repositories
{
    public interface ITimestreamRepository
    {
        public Task<DataTable> GetRecords(string database, string table, GridModel gridModel);
        public Task<DataTable> GetColumns(string database, string table);
    }
}