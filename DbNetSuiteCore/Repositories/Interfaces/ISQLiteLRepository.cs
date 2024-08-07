using TQ.Models;
using System.Data;

namespace DbNetTimeCore.Repositories
{
    public interface ISQLiteRepository
    {
        public Task<DataTable> GetRecords(GridModel gridModel);
        public Task<DataTable> GetColumns(GridModel gridModel);
    }
}