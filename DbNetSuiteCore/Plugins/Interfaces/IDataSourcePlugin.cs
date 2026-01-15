using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.Plugins.Interfaces
{
    public interface IDataSourcePlugin    
    {
        public object GetData(ComponentModel componentModel); 
    }
}