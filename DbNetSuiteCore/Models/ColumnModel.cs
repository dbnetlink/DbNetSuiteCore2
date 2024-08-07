using DbNetSuiteCore.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Text.Json.Serialization;

namespace TQ.Models
{
    public class ColumnModel
    {
        private List<string> _numericDataTypes = new List<string>() { nameof(Decimal), nameof(Double), nameof(Single), nameof(Int64), nameof(Int32), nameof(Int16) };
        private Type? _DataType = null;
        public string Label { get; set; } = string.Empty;
        public string Expression { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ColumnName => Name.Split(".").Last();
        public string Key => Name.GetHashCode().ToString();
        public bool IsPrimaryKey { get; set; } = false;
        public bool IsNumeric => _numericDataTypes.Contains(DataTypeName);
        public string DataTypeName => DataType.ToString().Split(".").Last();    
        [JsonIgnore]
        public Type DataType
        {
            get { return String.IsNullOrEmpty(UserDataType) ? _DataType ?? typeof(string) : Type.GetType($"System.{UserDataType}") ?? typeof(string); }
            set { 
                _DataType = value;

                if (string.IsNullOrEmpty(UserDataType)) {
                    UserDataType = value.Name;
                };
            }
        }
        public string UserDataType { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public bool Initialised { get; set; } = false;

        public ColumnModel()
        {
        }
         public ColumnModel(DataColumn dataColumn)
        {
            Expression = dataColumn.ColumnName;
            Label = TextHelper.GenerateLabel(dataColumn.ColumnName); 
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
