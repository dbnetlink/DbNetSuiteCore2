using System.Data;
using System.Text.Json.Serialization;

namespace DbNetSuiteCore.Models
{
    public class LookupModel
    {
        public string TableName { get; set; } = string.Empty;
        public string KeyColumn { get; set; } = string.Empty;
        public string DescriptionColumn { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty;
        public LookupModel() { }
        public LookupModel(string tableName, string keyColumn, string descriptionColumn, string? filter = null)
        {
            TableName = tableName;
            KeyColumn = keyColumn;
            DescriptionColumn = descriptionColumn;

            if (string.IsNullOrEmpty(filter) == false) 
            {
                Filter = filter;
            }
        }
    }
}
