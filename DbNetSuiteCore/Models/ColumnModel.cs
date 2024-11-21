using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using System.Data;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DbNetSuiteCore.Models
{
    public class ColumnModel
    {
        private List<string> _numericDataTypes = new List<string>() { nameof(Decimal), nameof(Double), nameof(Single), nameof(Int64), nameof(Int32), nameof(Int16), nameof(Byte), nameof(SByte) };
        private Type? _DataType = null;
        private List<KeyValuePair<string, string>>? _EnumOptions;
        public string Label { get; set; } = string.Empty;
        public string Expression { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ColumnName => Name.Split(".").Last();
        public string ColumnAlias => Expression.Contains(".") ? Expression.Replace(".","_") : Expression;
        public string Key { get; set; }
        public bool IsNumeric => _numericDataTypes.Contains(DataTypeName);
        public List<KeyValuePair<string, string>>? LookupOptions => (DbLookupOptions ?? EnumOptions);
        [JsonIgnore]
        public List<KeyValuePair<string, string>>? DbLookupOptions { get; set; } = null;
        [JsonIgnore]
        public Type? LookupEnum { get; set; }
        public string ParamName => $"Param{Ordinal}";
        public int Ordinal { get; set; }
        public List<KeyValuePair<string, string>>? EnumOptions
        {
            get
            {
                if (LookupEnum != null)
                {
                    _EnumOptions = EnumHelper.GetEnumOptions(LookupEnum!, DataType);
                };
                return _EnumOptions;
            }
            set { _EnumOptions = value; }
        }
        public string DataTypeName => DataType.ToString().Split(".").Last();
        [JsonIgnore]
        public Type DataType
        {
            get { return String.IsNullOrEmpty(UserDataType) ? _DataType ?? typeof(string) : Type.GetType($"System.{UserDataType}") ?? typeof(string); }
            set
            {
                _DataType = value;

                if (string.IsNullOrEmpty(UserDataType))
                {
                    UserDataType = value.Name;
                };
            }
        }
        public string DbDataType { get; set; } = string.Empty;
        public string UserDataType { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public bool Initialised { get; set; } = false;
        public bool Valid { get; set; } = true;
        public Lookup? Lookup { get; set; }
        public bool DataOnly { get; set; } = false;
        public bool PrimaryKey { get; set; } = false;
        public bool ForeignKey { get; set; } = false;
        internal bool Searchable => (DataType == typeof(string) && DbDataType != nameof(System.Data.SqlTypes.SqlXml));


        [JsonIgnore]
        public static List<KeyValuePair<string, string>> BooleanFilterOptions => new List<KeyValuePair<string, string>>()
        {
            new KeyValuePair<string, string>("1","Yes"),
            new KeyValuePair<string, string>("0","No"),
        };
        public ColumnModel()
        {
            Key = Guid.NewGuid().ToString().Split("-").First();
        }
        public ColumnModel(DataColumn dataColumn, DataSourceType dataSourceType) : this()
        {
            Expression = dataColumn.ColumnName;
            Label = TextHelper.GenerateLabel(dataColumn.ColumnName);
            Name = (dataSourceType == DataSourceType.Excel || dataSourceType == DataSourceType.JSON) ? dataColumn.ColumnName : CleanColumnName(dataColumn.ColumnName);
            DataType = dataColumn.DataType;
            Initialised = true;
            PrimaryKey = dataColumn.Unique;
        }

        public ColumnModel(DataRow dataRow) : this()
        {
            Expression = (string)dataRow["ColumnName"];
            Label = TextHelper.GenerateLabel((string)dataRow["ColumnName"]);
            Name = CleanColumnName((string)dataRow["ColumnName"]);
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

        public ColumnModel(string expression, string label) : this()
        {
            Expression = expression;
            Label = label;
        }

        public ColumnModel(string expression) : this()
        {
            Expression = expression;
            Label = TextHelper.GenerateLabel(expression);
        }

        public void Update(DataColumn dataColumn, DataSourceType dataSourceType)
        {
            DataType = dataColumn.DataType;
            Initialised = true;
            Name = (dataSourceType == DataSourceType.Excel ||dataSourceType ==  DataSourceType.JSON) ? dataColumn.ColumnName : CleanColumnName(dataColumn.ColumnName);
        }

        public void Update(DataRow dataRow)
        {
            DataType = (Type)dataRow["DataType"];
            Initialised = true;
            Name = CleanColumnName(string.IsNullOrEmpty((string)dataRow["ColumnName"]) ? Expression : (string)dataRow["ColumnName"]);
            if (string.IsNullOrEmpty(Label))
            {
                Label = TextHelper.GenerateLabel(Name);
            }
        }

        public string GetLookupValue(object value)
        {
            if (LookupOptions == null)
            {
                return string.Empty;
            }
            KeyValuePair<string, string>? option = LookupOptions!.FirstOrDefault(p => p.Key.ToString() == value.ToString());
            if (option.Value.Key != null)
            {
                return option.Value.Value;
            }
            else
            {
                return value?.ToString() ?? string.Empty;
            }
        }

        private string CleanColumnName(string columnName)
        {
            return new Regex("[^a-zA-Z0-9_]").Replace(columnName, string.Empty);
        }
    }
}
