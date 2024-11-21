using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface ISqlRepository
    {
        public Task GetRecords(ComponentModel componentModel);
        public Task<DataTable> GetColumns(ComponentModel componentModel);
        public Task GetRecord(ComponentModel gridModel);
        public Task UpdateRecord(FormModel formModel);
    }
}