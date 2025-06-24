using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface ISqlRepository
    {
        public Task GetRecords(ComponentModel componentModel);
        public Task<DataTable> GetColumns(ComponentModel componentModel);
        public Task GetRecord(ComponentModel gridModel);
        public Task<DataTable> GetRecordDataTable(ComponentModel componentModel);
        public Task<bool> PrimaryKeyExists(ComponentModel componentModel);
        public Task <bool> ValueIsUnique(FormModel formModel, GridFormColumn column);
        public Task GetLookupOptions(ComponentModel componentModel);
        public Task UpdateRecord(FormModel formModel);
        public Task UpdateRecords(GridModel gridModel);
        public Task DeleteRecord(FormModel formModel);
        public Task InsertRecord(FormModel formModel);
        public Task<List<object>> GetPrimaryKeyValues(GridModel gridModel);
    }
}