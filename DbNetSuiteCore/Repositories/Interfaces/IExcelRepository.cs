using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface IExcelRepository
    {
        public Task GetRecords(GridModel gridModel, HttpContext httpContext);
        public Task<DataTable> GetColumns(GridModel gridModel, HttpContext httpContext);
        public Task GetRecord(GridModel gridModel, HttpContext httpContext);
    }
}