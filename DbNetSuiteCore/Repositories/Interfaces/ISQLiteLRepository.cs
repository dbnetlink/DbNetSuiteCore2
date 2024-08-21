using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface ISQLiteRepository
    {
        public Task<DataTable> GetRecords(GridModel gridModel);
        public Task<DataTable> GetColumns(GridModel gridModel);
    }
}