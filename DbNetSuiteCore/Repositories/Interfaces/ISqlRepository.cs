using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface ISqlRepository
    {
        public Task GetRecords(GridModel gridModel);
        public Task<DataTable> GetColumns(GridModel gridModel);
        public Task GetRecord(GridModel gridModel);
    }
}