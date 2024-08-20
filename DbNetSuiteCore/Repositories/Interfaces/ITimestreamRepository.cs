using TQ.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface ITimestreamRepository
    {
        public Task<DataTable> GetRecords(GridModel gridModel);
        public Task<DataTable> GetColumns(GridModel gridModel);
    }
}