using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories.Interfaces;

namespace DbNetSuiteCore.Repositories
{
    public interface IExcelRepository : IRepository
    {
        public void GetRecord(ComponentModel componentModel);
    }
}