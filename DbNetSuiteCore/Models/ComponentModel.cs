using System.Data;
using DbNetSuiteCore.Enums;
using MongoDB.Bson;
using DbNetSuiteCore.Helpers;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;

namespace DbNetSuiteCore.Models
{
    public abstract class ComponentModel
    {
        protected RowSelection _RowSelection = RowSelection.None;
        private string _Url = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DataSourceType DataSourceType { get; set; }
        [JsonIgnore]
        internal DataTable Data { get; set; } = new DataTable();
        [JsonIgnore]
        public DataTable Record { get; set; } = new DataTable();
        public string TableName { get; set; } = string.Empty;
        public string ProcedureName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public List<DbParameter> ProcedureParameters { get; set; } = new List<DbParameter>();
        public string ConnectionAlias { get; set; } = string.Empty;
        [JsonProperty]
        internal bool IsStoredProcedure { get; set; } = false;
        [JsonProperty]
        internal Dictionary<string, List<string>> LinkedControlIds { get; set; } = new Dictionary<string, List<string>>();
        internal bool Uninitialised => GetColumns().Any() == false || GetColumns().Where(c => c.Initialised == false).Any();
        internal string SearchInput { get; set; } = string.Empty;
        internal string SortColumnName => SortColumn?.ColumnName ?? string.Empty;
        internal string SortColumnOrdinal => SortColumn?.Ordinal.ToString() ?? string.Empty;
        public bool Distinct { get; set; } = false;
        public string Caption { get; set; } = string.Empty;
        internal bool IgnoreSchemaTable { get; set; } = false;
        internal ColumnModel? PrimaryKeyColumn => GetColumns().FirstOrDefault(c => c.PrimaryKey);
        internal abstract IEnumerable<ColumnModel> SearchableColumns { get; }
        [JsonIgnore]
        internal LicenseInfo LicenseInfo { get; set; } = new LicenseInfo();
        internal List<SearchDialogFilter> SearchDialogFilter { get; set; } = new List<SearchDialogFilter>();
        [JsonProperty]
        internal SummaryModel? ParentModel { get; set; }
        [JsonProperty]
        internal SummaryModel? SummaryModel { get; set; }
        [JsonProperty]
        internal bool IsBlazor { get; set; } = false;
        [JsonProperty]
        internal bool ValidationPassed { get; set; } = false;
        [JsonIgnore]
        internal string RowId { get; set; } = string.Empty;
        public string Url
        {
            get
            {
                return _Url;
            }
            set
            {
                if (value.StartsWith("[") || value.StartsWith("{"))
                {
                    JSON = value;
                    _Url = string.Empty;
                }
                else
                {
                    _Url = value;
                }
            }
        }
        [JsonIgnore]
        internal string JSON { get; set; } = string.Empty;
        public Dictionary<string, string> ApiRequestHeaders { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> ApiRequestParameters { get; set; } = new Dictionary<string, string>();
        private List<ComponentModel> _LinkedControls { get; set; } = new List<ComponentModel>();

        public ComponentModel LinkedControl
        {
            set
            {
                AddLinkedControl(value);
            }
        }
        [JsonProperty]
        internal bool IsLinked { get; set; } = false;
        //public string ParentKey { get; set; } = string.Empty;
        public bool DeferredLoad { get; set; } = false;
        internal string HxFormTrigger => IsLinked || DeferredLoad ? "submit" : "load";
        internal string PostUrl => $"{this.GetType().Name.Replace("Model", "control").ToLower()}.htmx";
        internal string TriggerName { get; set; } = string.Empty;
        public string FixedFilter { get; set; } = string.Empty;
        public List<DbParameter> FixedFilterParameters { get; set; } = new List<DbParameter>();
        public bool Cache { get; set; } = false;
        public int QueryLimit { get; set; } = -1;
        public bool DiagnosticsMode { get; set; } = false;
        public bool Search { get; set; } = true;
        internal string SearchDialogConjunction { get; set; } = "and";
        public string Message = string.Empty;
        [JsonProperty]
        internal MessageType MessageType = MessageType.None;
        internal bool IsParent => LinkedControlIds.Any();
        [JsonIgnore]
        internal HttpContext? HttpContext { get; set; } = null;
        public ComponentModel()
        {
            Id = GeneratedId();
        }
        public ComponentModel(DataSourceType dataSourceType, string url) : this()
        {
            DataSourceType = dataSourceType;
            Url = url;
        }

        public ComponentModel(DataSourceType dataSourceType, string connectionAlias, string tableName, bool isStoredProcedure = false) : this()
        {
            DataSourceType = dataSourceType;
            ConnectionAlias = connectionAlias;
            TableName = isStoredProcedure ? string.Empty : tableName;
            ProcedureName = isStoredProcedure ? tableName : string.Empty;
            IsStoredProcedure = isStoredProcedure;
        }

        public ComponentModel(DataSourceType dataSourceType, string connectionAlias, string procedureName, List<DbParameter> procedureParameters) : this()
        {
            DataSourceType = dataSourceType;
            ConnectionAlias = connectionAlias;
            ProcedureName = procedureName;
            ProcedureParameters = procedureParameters;
            IsStoredProcedure = true;
        }

        public ComponentModel(string tableName) : this()
        {
            TableName = tableName;
        }

        internal DataColumn? GetDataColumn(ColumnModel column)
        {
            return Data.Columns.Cast<DataColumn>().FirstOrDefault(c => c.ColumnName.ToLower() == column.Name.ToLower() || c.ColumnName.ToLower() == column.ColumnName.ToLower() || c.ColumnName.ToLower() == column.Expression.ToLower());
        }

        internal ColumnModel? GetColumn(string columnName)
        {
            return GetColumns().FirstOrDefault(c => c.ColumnName.ToLower() == columnName.ToLower() || c.Name.ToLower() == columnName.ToLower() || c.Expression.ToLower() == columnName.ToLower());
        }

        internal object RowValue(DataRow dataRow, string columnName, object defaultValue)
        {
            var dataColumn = dataRow.Table.Columns.Cast<DataColumn>().ToList().FirstOrDefault(c => c.ColumnName == columnName);

            if (dataColumn != null)
            {
                return dataRow[dataColumn];
            }

            return defaultValue;
        }

        private string GeneratedId()
        {
            return $"{this.GetType().Name.Replace("Model", string.Empty)}{DateTime.Now.Ticks}";
        }

        internal void SetId()
        {
            Id = GeneratedId();
        }

        internal string ObfuscateColumnName(ColumnModel column)
        {
            return (string.IsNullOrEmpty(column.Alias) ? column.ColumnName : column.Alias).ToLower();
        }

        internal List<string> GetLinkedControlIds(string typeName)
        {
            return LinkedControlIds.ContainsKey(typeName) ? LinkedControlIds[typeName] : new List<string>();
        }

        internal List<object> GetPrimaryKeyValues()
        {
            return JsonConvert.DeserializeObject<List<object>>(TextHelper.DeobfuscateString(RowId)) ?? new List<object>();
        }
        internal List<object> GetParentKeyValues()
        {
            var primaryKeyValues = new List<object>();
            foreach (var column in ParentModel!.Columns.Where(c => c.PrimaryKey))
            {
                primaryKeyValues.Add(ParentModel!.ParentRow[column.Name]);
            }

            return primaryKeyValues;
        }

        internal List<string> GetLinkedControlIds()
        {
            return LinkedControlIds.SelectMany(d => d.Value).ToList();
        }

        public void AddLinkedControl(ComponentModel componentModel)
        {
            _LinkedControls.Add(componentModel);
            componentModel.IsLinked = true;

            var typeName = componentModel.GetType().Name;

            if (this.GetType().Name == nameof(GridModel) && componentModel.GetType().Name == nameof(FormModel))
            {
                if (this.TableName.Equals(componentModel.TableName, StringComparison.InvariantCultureIgnoreCase))
                {
                    (componentModel as FormModel)!.OneToOne = true;
                }
            }

            if (LinkedControlIds.ContainsKey(typeName) == false)
            {
                LinkedControlIds[typeName] = new List<string>();
            }
            LinkedControlIds[typeName].Add(componentModel.Id);

            AssignLinkedProperties(this, componentModel);

            foreach (ComponentModel linkedGrid in componentModel._LinkedControls)
            {
                AssignLinkedProperties(componentModel, linkedGrid);
            }
        }

        internal void AssignLinkedProperties(ComponentModel parent, ComponentModel child)
        {
            if (string.IsNullOrEmpty(child.ConnectionAlias))
            {
                child.ConnectionAlias = parent.ConnectionAlias;
                child.DataSourceType = parent.DataSourceType;
            }
        }

        internal string LookupColumnName(string columnName)
        {
            return GetColumns().FirstOrDefault(c => c.Alias.ToLower() == columnName.ToLower())?.ColumnName ?? columnName;
        }

        internal abstract IEnumerable<ColumnModel> GetColumns();
        internal abstract void SetColumns(IEnumerable<ColumnModel> columns);
        internal abstract ColumnModel NewColumn(DataRow dataRow, DataSourceType dataSourceType);
        internal abstract ColumnModel NewColumn(DataColumn dataColumn, DataSourceType dataSourceType);
        internal abstract ColumnModel NewColumn(BsonElement element);
        internal abstract ColumnModel? SortColumn { get; }
        internal abstract SortOrder? SortSequence { get; set; }
      
    }
}