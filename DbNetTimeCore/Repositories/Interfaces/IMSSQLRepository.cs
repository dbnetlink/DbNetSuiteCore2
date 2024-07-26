using DbNetTimeCore.Models;
using System.Data;

namespace DbNetTimeCore.Repositories
{
    public interface IMSSQLRepository
    {
        public Task<DataTable> GetRecords(GridModel gridModel);
        public Task<DataTable> GetColumns(GridModel gridModel);
    }
}