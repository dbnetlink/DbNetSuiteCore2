using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Plugins.Interfaces
{
    public interface IDataSourcePlugin    
    {
        public void GetData(ComponentModel componentModel);
        public void GetTreeData(TreeModel treeModel);
    }
}