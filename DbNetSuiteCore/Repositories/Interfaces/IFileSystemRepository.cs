using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface IFileSystemRepository
    {
        public Task GetRecords(ComponentModel componentModel, HttpContext httpContext);
        public Task<DataTable> GetColumns(ComponentModel componentModel, HttpContext httpContext);
    }
}