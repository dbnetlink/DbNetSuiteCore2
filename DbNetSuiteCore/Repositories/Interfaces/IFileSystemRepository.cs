using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface IFileSystemRepository
    {
        public void GetRecords(ComponentModel componentModel);
        public DataTable GetColumns(ComponentModel componentModel);
    }
}