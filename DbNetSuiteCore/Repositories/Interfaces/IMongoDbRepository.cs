using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface IMongoDbRepository
    {
        public Task GetRecords(ComponentModel componentModel);
        public Task<DataTable> GetColumns(ComponentModel componentModel);
        public Task GetRecord(GridModel gridModel);
      }
}