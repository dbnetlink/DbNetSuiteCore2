using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface IExcelRepository
    {
        public void GetRecords(GridSelectModel gridSelectModel);
        public DataTable GetColumns(GridSelectModel gridSelectModel);
        public void GetRecord(GridSelectModel gridSelectModel);
    }
}