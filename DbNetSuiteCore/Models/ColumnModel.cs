using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Repositories;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MongoDB.Bson;
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
        public string ColumnAlias => Expression.Contains(".") ? Expression.Replace(".", "_") : Expression;
        public string Key { get; set; }
        public string BaseTableName { get; set; }
        public bool IsNumeric => _numericDataTypes.Contains(DataTypeName);
        public List<KeyValuePair<string, string>>? LookupOptions => GetLookupOptions();
        // [JsonIgnore]
        public List<KeyValuePair<string, string>>? DbLookupOptions { get; set; } = null;
        [JsonIgnore]
        public Type? LookupEnum { get; set; }
        public List<string>? LookupList { get; set; }
        public Dictionary<string,string>? LookupDictionary { get; set; }
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
        internal bool Searchable => (DataType == typeof(string) && DbDataType != "xml");
        public SortOrder? InitialSortOrder { get; set; } = null;
        public bool LookupNotPopulated => (Lookup != null && LookupOptions == null);
        public string EnumName { get; set; } = string.Empty;


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
            Label = TextHelper.GenerateLabel(dataColumn.ColumnName);
            Expression = dataColumn.ColumnName;
            this.Update(dataColumn, dataSourceType);
        }

        public ColumnModel(DataRow dataRow, DataSourceType dataSourceType) : this()
        {
            Expression = (string)dataRow["ColumnName"];
            Update(dataRow, dataSourceType);
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

        public ColumnModel(BsonElement element) : this(element.Name)
        {
            if (element.Name == MongoDbRepository.PrimaryKeyName)
            {
                PrimaryKey = true;
            }
        }

        private List<KeyValuePair<string, string>>? GetLookupOptions()
        {
            return ((DbLookupOptions ?? EnumOptions) ?? GetListOptions()) ?? GetDictionaryOptions();
        }

        private List<KeyValuePair<string, string>>? GetListOptions()
        {
            if (LookupList == null)
            {
                return null;
            }
            return LookupList.OrderBy(o => o).Select(o =>  new KeyValuePair<string, string>(o, o)).ToList();
        }

        private List<KeyValuePair<string, string>>? GetDictionaryOptions()
        {
            if (LookupDictionary == null)
            {
                return null;
            }
            return LookupDictionary.OrderBy(o => o.Value).Select(o => new KeyValuePair<string, string>(o.Key, o.Value)).ToList();
        }


        public void Update(DataColumn dataColumn, DataSourceType dataSourceType)
        {
            DataType = dataColumn.DataType;
            Initialised = true;
            Name = (dataSourceType == DataSourceType.Excel || dataSourceType == DataSourceType.JSON) ? dataColumn.ColumnName : CleanColumnName(dataColumn.ColumnName);

            switch (dataSourceType)
            {
                case DataSourceType.MongoDB:
                    PrimaryKey = (Name == MongoDbRepository.PrimaryKeyName);
                    break;
            }
        }

        public void Update(DataRow dataRow, DataSourceType dataSourceType)
        {
            try
            {
                DataType = (Type)dataRow["DataType"];

                switch (dataSourceType)
                {
                    case DataSourceType.MSSQL:
                        IsSupportedType<MSSQLDataTypes>(dataRow["DataTypeName"]);
                        break;
                    case DataSourceType.MySql:
                        IsSupportedType<MySqlDataTypes>(dataRow["ProviderType"]);
                        break;
                    case DataSourceType.PostgreSql:
                        IsSupportedType<PostgreSqlDataTypes>(dataRow["ProviderType"]);
                        if (DbDataType == PostgreSqlDataTypes.Enum.ToString())
                        {
                            EnumName = dataRow["DataTypeName"].ToString();
                        }
                        break;
                    case DataSourceType.SQLite:
                        DbDataType = dataRow["DataTypeName"].ToString();
                        switch (DbDataType)
                        {
                            case "DATETIME":
                            case "DATE":
                                UserDataType = nameof(DateTime);
                                break;
                        }
                        break;
                    default:
                        DbDataType = dataRow["DataTypeName"].ToString();
                        break;
                }

                BaseTableName = dataRow["BaseTableName"].ToString();
            }
            catch (Exception)
            {
                Valid = false;
            }
            Initialised = true;
            Name = CleanColumnName(string.IsNullOrEmpty((string)dataRow["ColumnName"]) ? Expression : (string)dataRow["ColumnName"]);
            if (string.IsNullOrEmpty(Label))
            {
                Label = TextHelper.GenerateLabel(Name);
            }
            PrimaryKey = (bool)dataRow["IsKey"] || (bool)dataRow["IsAutoincrement"];
            if (this is FormColumn)
            {
                var formColumn = (FormColumn)this;
                if (formColumn.Required == false)
                {
                    formColumn.Required = (bool)dataRow["AllowDBNull"] == false;
                }
                formColumn.Autoincrement = (bool)dataRow["IsAutoincrement"];
                if (formColumn.MaxLength.HasValue == false)
                {
                    formColumn.MaxLength = (int)dataRow["ColumnSize"];
                }
            }
        }

        public void Update(BsonValue bsonValue)
        {
            if (string.IsNullOrEmpty(DbDataType) == false || bsonValue.BsonType == BsonType.Null)
            {
                return;
            }

            DbDataType = bsonValue.BsonType.ToString();

            if (DbDataType == nameof(BsonType.Document))
            {
                Valid = false;
            }

            if (this is FormColumn)
            {
                var formColumn = (FormColumn)this;
                if (formColumn.DbDataType == nameof(BsonType.Array))
                {
                    if (formColumn.ControlType == FormControlType.Auto)
                    {
                        formColumn.ControlType = FormControlType.TextArea;
                    }
                }
            }
        }

        private void IsSupportedType<T>(object value) where T : Enum
        {
            T enumValue = (T)Enum.Parse(typeof(T), value.ToString(), ignoreCase: true);

            if (Enum.IsDefined(typeof(T), enumValue))
            {
                DbDataType = enumValue.ToString();
            }
            else
            {
                Valid = false;
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
