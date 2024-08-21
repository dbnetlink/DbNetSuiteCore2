using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface IJSONRepository
    {
        public Task<DataTable> GetRecords(GridModel gridModel, HttpContext httpContext);
        public Task<DataTable> GetColumns(GridModel gridModel, HttpContext httpContext);
    }
}