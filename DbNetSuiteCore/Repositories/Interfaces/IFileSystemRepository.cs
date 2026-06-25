using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories.Interfaces;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface IFileSystemRepository : IRepository
    {
        DataTable GetEmptyDataTable();
        DataTable GetFolderContents(string path, TreeModel treeModel);
    }
}