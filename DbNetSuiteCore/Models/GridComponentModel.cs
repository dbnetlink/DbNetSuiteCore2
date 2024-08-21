using DbNetSuiteCore.Enums;

namespace DbNetSuiteCore.Models
{
    public class GridComponentModel : ComponentModel
    {
        public string Id => $"{TableName.Replace(".",string.Empty)}Grid";
        public string DatabaseName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public DataSourceType DataSourceType { get; set; }    
    }
}