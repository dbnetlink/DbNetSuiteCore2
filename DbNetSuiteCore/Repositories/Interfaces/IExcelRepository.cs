using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface IExcelRepository
    {
        public Task GetRecords(GridModel gridModel);
        public Task<DataTable> GetColumns(GridModel gridModel);
        public Task GetRecord(GridModel gridModel);
    }
}