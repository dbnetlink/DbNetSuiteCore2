using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface IMongoDbRepository
    {
        public Task GetRecords(ComponentModel componentModel);
        public Task<DataTable> GetColumns(ComponentModel componentModel);
        public Task GetRecord(ComponentModel componentModel);
        public Task UpdateRecord(FormModel formModel);
        public Task UpdateRecords(GridModel gridModel);
        public Task InsertRecord(FormModel formModel);
        public Task DeleteRecord(FormModel formModel);
        public Task<List<object>> GetPrimaryKeyValues(GridModel gridModel);
    }
}