using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Enums;
using System.Data;
using System.Text.Json.Serialization;

namespace DbNetSuiteCore.Models
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
        public bool IsNumeric => _numericDataTypes.Contains(DataTypeName);
        public Type? LookupEnum { get; set; } 
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
        public string DbDataType { get; set; } = string.Empty;
        public string UserDataType { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public bool Initialised { get; set; } = false;
        public bool Valid { get; set; } = true;
        public ColumnModel()
        {
        }
        public ColumnModel(DataColumn dataColumn)
        {
            Expression = dataColumn.ColumnName;
            Label = TextHelper.GenerateLabel(dataColumn.ColumnName); 
            Name = dataColumn.ColumnName;
            DataType = dataColumn.DataType;
            Initialised = true;
        }

        public ColumnModel(DataRow dataRow,DataSourceType dataSourceType)
        {
            Expression = QualifyExpression((string)dataRow["ColumnName"], dataSourceType);
            Label = TextHelper.GenerateLabel((string)dataRow["ColumnName"]);
            Name = (string)dataRow["ColumnName"];
            try
            {
                DataType = (Type)dataRow["DataType"];
                DbDataType = ((Type)dataRow["ProviderSpecificDataType"]).Name;
            }
            catch (Exception)
            {
                Valid = false;
            }
            Initialised = true;
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

        public void Update(DataColumn dataColumn)
        {
            DataType = dataColumn.DataType;
            Initialised = true;
            Name = dataColumn.ColumnName;
            if (string.IsNullOrEmpty(Label))
            {
                Label = TextHelper.GenerateLabel(Name);
            }
        }

        public void Update(DataRow dataRow)
        {
            DataType = (Type)dataRow["DataType"];
            Initialised = true;
            Name = (string)dataRow["ColumnName"];
            if (string.IsNullOrEmpty(Label))
            {
                Label = TextHelper.GenerateLabel(Name);
            }
        }

        private string QualifyExpression(string columnName, DataSourceType dataSourceType)
        {
            switch(dataSourceType)
            {
                case DataSourceType.MSSQL:
                    return $"[{columnName}]";
                default:
                    return columnName;  
            }
        }
    }
}
