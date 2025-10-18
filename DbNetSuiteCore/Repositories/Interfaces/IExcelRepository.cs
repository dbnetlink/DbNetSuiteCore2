using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface IExcelRepository
    {
        public void GetRecords(ComponentModel componentModel);
        public DataTable GetColumns(ComponentModel componentModel);
        public void GetRecord(ComponentModel componentModel);
    }
}