using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface IFileSystemRepository
    {
        void GetRecords(ComponentModel componentModel);
        DataTable GetColumns(ComponentModel componentModel);
        DataTable GetEmptyDataTable();
        DataTable GetFolderContents(string path, TreeModel treeModel);
    }
}