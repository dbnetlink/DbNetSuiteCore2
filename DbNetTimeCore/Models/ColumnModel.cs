using System.Data;
using System.Text.Json.Serialization;

namespace DbNetTimeCore.Models
{
    public class ColumnModel
    {
        public string Label { get; set; } = string.Empty;
        public string Expression { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ColumnName => Name.Split(".").Last();
        public string Key => Name.GetHashCode().ToString();
        public bool IsPrimaryKey { get; set; } = false;
        [JsonIgnore]
        public Type DataType { get; set; } = typeof(String);
        public string Format { get; set; } = string.Empty;
        public bool Initialised { get; set; } = false;

        public ColumnModel()
        {
        }
         public ColumnModel(DataColumn dataColumn)
        {
            Expression = dataColumn.ColumnName;
            Label = dataColumn.ColumnName;
            Name = dataColumn.ColumnName;
            DataType = dataColumn.DataType;
        }
        public ColumnModel(string expression, string label)
        {
            Expression = expression;
            Label = label;
        }

        public ColumnModel(string expression) 
        {
            Expression = expression;
            Label = expression.Split(" ").Last();
        }
    }
}
