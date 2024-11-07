using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface IJSONRepository
    {
        public Task GetRecords(ComponentModel componentModel, HttpContext httpContext);
        public Task<DataTable> GetColumns(GridModel gridModel, HttpContext httpContext);
        public Task GetRecord(GridModel gridModel, HttpContext httpContext);
        public Task<DataTable> GetColumns(ComponentModel componentModel, HttpContext httpContext);
    }
}