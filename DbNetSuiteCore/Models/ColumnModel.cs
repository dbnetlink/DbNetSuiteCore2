using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Repositories;
using Microsoft.AspNetCore.Html;
using Microsoft.IdentityModel.Tokens;
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
        private SearchControlType _searchControlType = SearchControlType.Text;
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
        public IEnumerable<Int32>? LookupRange { get; set; }
        public Dictionary<string, string>? LookupDictionary { get; set; }
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
        public bool DistinctLookup => Lookup != null && string.IsNullOrEmpty(Lookup.TableName);
        public bool SearchLookup => Lookup != null && string.IsNullOrEmpty(Lookup.TableName) == false;
        public bool DataOnly { get; set; } = false;
        public bool PrimaryKey { get; set; } = false;
        public bool ForeignKey { get; set; } = false;
        internal bool StringSearchable => (DataType == typeof(string) && DbDataType != "xml");
        public SortOrder? InitialSortOrder { get; set; } = null;
        public bool LookupNotPopulated => (Lookup != null && LookupOptions == null);
        public string EnumName { get; set; } = string.Empty;
        public bool AllowDBNull { get; set; } = true;
        public bool Search { get; set; } = true;
        public bool IsSearchable => DataType != typeof(Byte[]) && Search && SearchableDataType() && ForeignKey == false;
        public SearchControlType SearchControlType
        {
            get
            {
                if (_searchControlType == SearchControlType.Text)
                {
                    if (IsNumeric)
                    {
                        return SearchControlType.Number;
                    }
                    else
                    {
                        switch (DataTypeName)
                        {
                            case nameof(DateTime):
                            case nameof(DateTimeOffset):
                                return SearchControlType.Date;
                            case nameof(TimeSpan):
                                return SearchControlType.Time;
                        }
                    }
                }

                return _searchControlType;
            }
            set
            {
                if (_searchControlType == SearchControlType.Text)
                {
                    _searchControlType = value;
                };
            }
        }

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
            Expression = (string)RowValue(dataRow, "ColumnName", string.Empty);
            Update(dataRow, dataSourceType, true);
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
            return ((DbLookupOptions ?? EnumOptions) ?? GetListOptions()) ?? GetDictionaryOptions() ?? GetRangeOptions();
        }

        private List<KeyValuePair<string, string>>? GetListOptions()
        {
            if (LookupList == null)
            {
                return null;
            }
            return LookupList.OrderBy(o => o).Select(o => new KeyValuePair<string, string>(o, o)).ToList();
        }

        private List<KeyValuePair<string, string>>? GetRangeOptions()
        {
            if (LookupRange == null)
            {
                return null;
            }
            return LookupRange.Select(o => new KeyValuePair<string, string>(o.ToString(), o.ToString())).ToList();
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

            if (this is FormColumn)
            {
                var formColumn = (FormColumn)this;
                switch (dataSourceType)
                {
                    case DataSourceType.MongoDB:
                        PrimaryKey = (Name == MongoDbRepository.PrimaryKeyName);
                        formColumn.Autoincrement = (Name == MongoDbRepository.PrimaryKeyName);
                        break;
                }
            }
        }
        public string ToStringOrEmpty(object? value)
        {
            return value?.ToString() ?? string.Empty;
        }

        public void Update(DataRow dataRow, DataSourceType dataSourceType, bool generated = false)
        {
            try
            {
                switch (dataSourceType)
                {
                    case DataSourceType.SQLite:
                        break;
                    default:
                        DataType = (Type)dataRow["DataType"];
                        break;
                }

                string dataTypeName = (string)RowValue(dataRow, "DataTypeName", string.Empty);
                int providerType = (int)RowValue(dataRow, "ProviderType", 0);

                switch (dataSourceType)
                {
                    case DataSourceType.MSSQL:
                        IsSupportedType<MSSQLDataTypes>(providerType);
                        break;
                    case DataSourceType.MySql:
                        IsSupportedType<MySqlDataTypes>(providerType);
                        break;
                    case DataSourceType.PostgreSql:
                        IsSupportedType<PostgreSqlDataTypes>(providerType);
                        if (DbDataType == PostgreSqlDataTypes.Enum.ToString())
                        {
                            EnumName = dataTypeName;
                        }
                        break;
                    case DataSourceType.SQLite:
                        DbDataType = dataTypeName;
                        if (string.IsNullOrEmpty(UserDataType) == false)
                        {
                            break;
                        }
                        switch (DbDataType)
                        {
                            case nameof(SQLiteDataTypes.DATETIME):
                            case nameof(SQLiteDataTypes.DATE):
                            case nameof(SQLiteDataTypes.TIMESTAMP):
                                UserDataType = nameof(DateTime);
                                break;
                            case nameof(SQLiteDataTypes.NUMERIC):
                                UserDataType = nameof(Decimal);
                                break;
                            case nameof(SQLiteDataTypes.REAL):
                                UserDataType = nameof(Double);
                                break;
                            default:
                                DataType = (Type)dataRow["DataType"];
                                break;
                        }
                        break;
                    case DataSourceType.Oracle:
                        DbDataType = ((OracleDataTypes)providerType).ToString();
                        break;
                    default:
                        DbDataType = dataTypeName;
                        break;
                }

                BaseTableName = (string)RowValue(dataRow, "BaseTableName", string.Empty);
            }
            catch (Exception)
            {
                Valid = false;
            }
            Initialised = true;

            string columnName = (string)RowValue(dataRow, "ColumnName", string.Empty);

            Name = CleanColumnName(string.IsNullOrEmpty(columnName) ? Expression : columnName);
            if (string.IsNullOrEmpty(Label))
            {
                Label = TextHelper.GenerateLabel(Name);
            }

            bool isAutoincrement = (bool)RowValue(dataRow, "IsAutoincrement", false);
            bool isPrimaryKey = (bool)RowValue(dataRow, "IsKey", false) || isAutoincrement;

            if (isPrimaryKey && PrimaryKey == false)
            {
                PrimaryKey = true;
            }

            AllowDBNull = (bool)RowValue(dataRow, "AllowDBNull", true);
            if (this is FormColumn)
            {
                var formColumn = (FormColumn)this;
                if (formColumn.Required == false)
                {
                    formColumn.Required = (bool)RowValue(dataRow, "AllowDBNull", true) == false;
                }
                if (formColumn.Autoincrement == false)
                {
                    formColumn.Autoincrement = isAutoincrement;
                }
                if (formColumn.MaxLength.HasValue == false && formColumn.DataType == typeof(String))
                {
                    int columnSize = (int)RowValue(dataRow, "ColumnSize", -1);
                    if (columnSize > 0)
                    {
                        formColumn.MaxLength = columnSize;
                    }
                }
                switch (dataSourceType)
                {
                    case DataSourceType.Oracle:
                        if (string.IsNullOrEmpty(formColumn.SequenceName) == false)
                        {
                            formColumn.Autoincrement = true;
                        }
                        break;
                }
            }
        }

        public bool AffinityDataType()
        {
            switch (DbDataType)
            {
                case nameof(SQLiteDataTypes.DATETIME):
                case nameof(SQLiteDataTypes.DATE):
                case nameof(SQLiteDataTypes.NUMERIC):
                case nameof(SQLiteDataTypes.REAL):
                case nameof(SQLiteDataTypes.TIMESTAMP):
                    return true;
            }
            return false;
        }

        private object RowValue(DataRow dataRow, string name, object defaultValue)
        {
            if (dataRow.Table.Columns.Contains(name) == false || dataRow[name] == null || dataRow[name] == DBNull.Value)
            {
                return defaultValue;
            }

            return dataRow[name];
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

        private void IsSupportedType<T>(int value) where T : Enum
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
        protected string DataList(Dictionary<string, string> attributes)
        {
            List<string> dataList = new List<string>();
            if (LookupOptions != null)
            {
                attributes["list"] = $"{attributes["id"]}_datalist";
                dataList.Add($"<datalist id=\"{attributes["list"]}\">");
                dataList.AddRange(OptionsList());
                dataList.Add($"</datalist>");
            }

            return string.Join(string.Empty, dataList);
        }

        protected List<string> OptionsList(List<string>? values = null, bool dataList = true)
        {
            List<string> options = new List<string>();

            if (dataList == false)
            {
                options.Add("<option value=\"\"></option >");
            }

            foreach (var option in LookupOptions ?? new List<KeyValuePair<string, string>>())
            {
                options.Add($"<option value=\"{option.Key}\" {((values ?? new List<string>()).Contains(option.Key) ? "selected" : "")}>{option.Value}</option>");
            }
            return options;
        }


        internal string GetDateTimeFormat(string inputType)
        {
            switch (DataType.Name)
            {
                case nameof(DateTime):
                    return (inputType == nameof(FormControlType.DateTime)) ? "yyyy-MM-dd'T'HH:mm" : "yyyy-MM-dd";
                case nameof(DateTimeOffset):
                    return (inputType == nameof(FormControlType.DateTime)) ? "yyyy-MM-dd'T'HH:mm" : "yyyy-MM-dd";
                case nameof(TimeSpan):
                    return (inputType == nameof(FormControlType.TimeWithSeconds)) ? @"hh\:mm\:ss" : @"hh\:mm";
            }

            return string.Empty;
        }

        public HtmlString SearchOperatorSelection(DataSourceType dataSourceType)
        {
            var attributes = new Dictionary<string, string>();
            attributes["name"] = $"searchDialogOperator";
            attributes["class"] = $"search-operator";
            List<string> select = new List<string>();
            select.Add($"<select {RazorHelper.Attributes(attributes)}><option/>");
            var options = SearchOperatorOptions(dataSourceType);

            if (DataType == typeof(bool) && EnumOptions != null)
            {
                select.AddRange(EnumOptions.Select(o => $"<option value=\"{(o.Key == "1" ? "True" : "False")}\">{o.Value}</option>").ToList());
            }
            else
            {
                select.AddRange(options.Select(o => $"<option value=\"{o.ToString()}\">{ResourceHelper.GetResourceString(o)}</option>").ToList());
            }
            select.Add("</select>");

            return new HtmlString(string.Join(string.Empty, select));
        }

        public HtmlString SearchInput()
        {
            var attributes = new Dictionary<string, string>();
            attributes["name"] = "searchDialogValue1";
            attributes["type"] = "hidden";
            List<string> input = new List<string>();

            if (DataType == typeof(bool))
            {
                input.Add($"<input {RazorHelper.Attributes(attributes)}/>");
                attributes["name"] = attributes["name"].Replace("1", "2");
                input.Add($"<input {RazorHelper.Attributes(attributes)}/>");
                return new HtmlString(string.Join(string.Empty, input));
            }

            bool supportsBetween = !SearchLookup;

            attributes["style"] = "width:130px;";
            attributes["data-datatype"] = DataTypeName;
            attributes["class"] = "first";
            attributes["type"] = SearchControlType.ToString().ToLower();

            if (attributes["type"] == "text")
            {
                attributes["style"] = "width:295px;";
                supportsBetween = false;
            }



            input.Add($"<div style=\"display:flex;flex-direction:row;align-items:center;gap:5px;\">");
            string lookup = string.Empty;
            if (DistinctLookup)
            {
                attributes["id"] = $"{Key}";
                lookup = DataList(attributes);
            }
            if (SearchLookup)
            {
                attributes["type"] = "text";
                attributes["style"] = "width:257px;";
                attributes["readonly"] = "readonly";
                lookup = RazorHelper.IconButton("List", IconHelper.List(), new Dictionary<string, string>() { { "data-key", Key }, { "class", "first" } }).ToString();
            }

            input.Add($"<input {RazorHelper.Attributes(attributes)}/>{lookup}");

            attributes["name"] = $"searchDialogValue2";
            if (supportsBetween)
            {
                attributes["class"] = "between hidden";
                input.Add($"<span class=\"between hidden\"> and </span>");
                input.Add($"<input {RazorHelper.Attributes(attributes)}/>");
            }
            else
            {
                attributes["type"] = "hidden";
                input.Add($"<input {RazorHelper.Attributes(attributes)}/>");
            }

            input.Add("</div>");

            return new HtmlString(string.Join(string.Empty, input));
        }


        private List<SearchOperator> SearchOperatorOptions(DataSourceType dataSourceType)
        {
            var options = new List<SearchOperator>();

            foreach (SearchOperator searchOperator in Enum.GetValues(typeof(SearchOperator)))
            {
                switch (searchOperator)
                {
                    case SearchOperator.In:
                    case SearchOperator.NotIn:
                        if (IsLookup())
                        {
                            options.Add(searchOperator);
                        }
                        break;
                    case SearchOperator.IsEmpty:
                    case SearchOperator.IsNotEmpty:
                        if (AllowDBNull && dataSourceType != DataSourceType.FileSystem)
                        {
                            options.Add(searchOperator);
                        }
                        break;
                    case SearchOperator.True:
                    case SearchOperator.False:
                        if (DataType == typeof(bool) && dataSourceType != DataSourceType.FileSystem)
                        {
                            options.Add(searchOperator);
                        }
                        break;
                    case SearchOperator.Contains:
                    case SearchOperator.DoesNotContain:
                    case SearchOperator.StartsWith:
                    case SearchOperator.DoesNotStartWith:
                    case SearchOperator.EndsWith:
                    case SearchOperator.DoesNotEndWith:
                        if (DataType == typeof(string) && IsLookup() == false)
                        {
                            options.Add(searchOperator);
                        }
                        break;
                    case SearchOperator.EqualTo:
                    case SearchOperator.NotEqualTo:
                        if (DataType != typeof(bool) && IsLookup() == false)
                        {
                            options.Add(searchOperator);
                        }
                        break;
                    default:
                        if (DataType != typeof(string) && DataType != typeof(bool) && IsLookup() == false)
                        {
                            options.Add(searchOperator);
                        }
                        break;
                }
            }

            return options;

            bool IsLookup()
            {
                return Lookup != null && DistinctLookup == false;
            }
        }

        private void AddOperator(SearchOperator searchOperator, List<SearchOperator> options)
        {
            if (IsNumeric)
            {
                if (IsNonStringOperator(searchOperator) == false)
                {
                    return;
                }
            }
            else
            {
                switch (DataTypeName)
                {
                    case nameof(Boolean):
                        if (IsBooleanOperator(searchOperator) == false)
                        {
                            return;
                        }
                        break;
                    case nameof(DateTime):
                    case nameof(DateTimeOffset):
                    case nameof(TimeSpan):
                        if (IsNonStringOperator(searchOperator) == false)
                        {
                            return;
                        }
                        break;
                    default:
                        if (IsStringOperator(searchOperator) == false)
                        {
                            return;
                        }
                        break;
                }
            }

            options.Add(searchOperator);
        }

        private Boolean IsBooleanOperator(SearchOperator searchOperator)
        {
            switch (searchOperator)
            {
                case SearchOperator.True:
                case SearchOperator.False:
                    return true;
                case SearchOperator.IsEmpty:
                case SearchOperator.IsNotEmpty:
                    if (AllowDBNull)
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }

        private Boolean IsStringOperator(SearchOperator searchOperator)
        {
            switch (searchOperator)
            {
                case SearchOperator.EqualTo:
                case SearchOperator.Contains:
                case SearchOperator.StartsWith:
                case SearchOperator.EndsWith:
                case SearchOperator.NotEqualTo:
                case SearchOperator.DoesNotContain:
                case SearchOperator.DoesNotEndWith:
                case SearchOperator.DoesNotStartWith:
                case SearchOperator.IsEmpty:
                case SearchOperator.IsNotEmpty:
                    return true;
            }

            return false;
        }

        private Boolean IsNonStringOperator(SearchOperator searchOperator)
        {
            switch (searchOperator)
            {
                case SearchOperator.EqualTo:
                case SearchOperator.NotEqualTo:
                case SearchOperator.GreaterThan:
                case SearchOperator.LessThan:
                case SearchOperator.Between:
                case SearchOperator.NotBetween:
                case SearchOperator.NotLessThan:
                case SearchOperator.NotGreaterThan:
                case SearchOperator.IsEmpty:
                case SearchOperator.IsNotEmpty:
                    return true;
            }

            return false;
        }

        private bool SearchableDataType()
        {
            switch (DbDataType)
            {
                case nameof(MSSQLDataTypes.Xml):
                case nameof(MSSQLDataTypes.Sql_Variant):
                    return false;
            }

            return true;
        }
    }
}