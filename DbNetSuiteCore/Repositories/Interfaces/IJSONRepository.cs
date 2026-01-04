using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface IJSONRepository
    {
        public Task GetRecords(GridSelectModel gridSelectModel, HttpContext httpContext);
        public Task GetRecord(GridSelectModel gridSelectModel, HttpContext httpContext);
        public Task<DataTable> GetColumns(GridSelectModel gridSelectModel, HttpContext httpContext);
        public void UpdateApiRequestParameters(GridSelectModel gridSelectModel, HttpContext context);
    }
}