using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Plugins.Interfaces;
using MongoDB.Bson;
using Newtonsoft.Json;
using System.Data;

namespace DbNetSuiteCore.Models
{
    public abstract class ComponentModel
    {
        protected RowSelection _RowSelection = RowSelection.None;
        private string _Url = string.Empty;
        [JsonProperty]
        internal string Id { get; set; } = string.Empty;
        /// <summary>
        /// User assignable name that can be used to help reference controls on a page
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Defines the type of data that the control will access
        /// </summary>
        public DataSourceType DataSourceType { get; set; }
        [JsonIgnore]
        public DataTable Data { get; set; } = new DataTable();
        [JsonIgnore]
        internal DataTable Record { get; set; } = new DataTable();
        /// <summary>
        /// For the form control this should be a table name, for grid and select controls this can also be a view or multiple tables/views with required join information.
        /// </summary>
        /// <remarks>
        /// An example of using mutliple tables and/or views would be "Customer join Address on Customer.Address_Id == Address.Address_Id join City on City.City_Id = Address.City_Id"        
        /// /// </remarks> 
        public string TableName { get; set; } = string.Empty;
        /// <summary>
        /// The name of the database. MongoDB only.
        /// </summary>
        public string DatabaseName { get; set; } = string.Empty;
        /// <summary>
        /// The connection alias stored in appSetting.json or environment variables
        /// </summary>
        public string ConnectionAlias { get; set; } = string.Empty;

        [JsonProperty]
        internal Dictionary<string, List<string>> LinkedControlIds { get; set; } = new Dictionary<string, List<string>>();
        internal bool Uninitialised => GetColumns().Any() == false || GetColumns().Where(c => c.Initialised == false).Any();
        internal string SearchInput { get; set; } = string.Empty;
        internal string SortColumnName => SortColumn?.ColumnName ?? string.Empty;
        internal string SortColumnOrdinal => SortColumn?.Ordinal.ToString() ?? string.Empty;
        /// <summary>
        /// Specifies a caption for the control
        /// </summary>
        public string Caption { get; set; } = string.Empty;
        internal bool IgnoreSchemaTable { get; set; } = false;
        internal ColumnModel PrimaryKeyColumn => GetColumns().FirstOrDefault(c => c.PrimaryKey);
        internal abstract IEnumerable<ColumnModel> SearchableColumns { get; }
        [JsonIgnore]
        internal LicenseInfo LicenseInfo { get; set; } = new LicenseInfo();
        internal List<SearchDialogFilter> SearchDialogFilter { get; set; } = new List<SearchDialogFilter>();
        [JsonProperty]
        internal SummaryModel ParentModel { get; set; }
        [JsonProperty]
        internal SummaryModel SummaryModel { get; set; }
        [JsonProperty]
        internal bool IsBlazor { get; set; } = false;
        [JsonProperty]
        internal bool ValidationPassed { get; set; } = false;
        [JsonIgnore]
        internal string RowId { get; set; } = string.Empty;
        /// <summary>
        /// Specifies a Url for a JSON file or API endpoint that returns JSON to be used as the data source.
        /// </summary>
        /// <remarks>
        /// A serialized JSON string can be assigned directly to this property.
        /// <remarks>
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
        private List<ComponentModel> _LinkedControls { get; set; } = new List<ComponentModel>();
        /// <summary>
        /// Used to assign linked child control(s) to this control.
        /// </summary>
        /// <remarks>
        /// The parent control should specify a primary key column and the child control(s) should specify a foreign key column that references the parent primary key column.
        /// </remarks>
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
        /// <summary>
        /// When set to true the control will only load when it becomes visibile.
        /// </summary>
        /// <remarks>
        /// This is useful when working with tabbed interfaces where controls are placed on tabs that are not initially visible.   
        /// </remarks>

        public bool DeferredLoad { get; set; } = false;
        internal string HxFormTrigger => IsLinked || DeferredLoad ? "submit" : "load";
        internal string PostUrl => $"{this.GetType().Name.Replace("Model", "control").ToLower()}{DbNetSuiteCore.Middleware.DbNetSuiteCore.Extension}";
        internal string TriggerName { get; set; } = string.Empty;
        /// <summary>
        /// Allows a fixed filter to be applied to the data source query.
        /// </summary>
        /// <remarks>
        /// Can used in conjunction with FixedFilterParameters to provide parameterised filtering.
        /// </remarks>
        public string FixedFilter { get; set; } = string.Empty;
        /// <summary>
        /// Specifies parameters to be used in conjunction with the FixedFilter property.
        /// </summary>
        public List<DbParameter> FixedFilterParameters { get; set; } = new List<DbParameter>();
        /// <summary>
        /// Allows custom headers to be added to API requests when using an API JSON data source.
        /// </summary>
        public Dictionary<string, string> ApiRequestHeaders { get; set; } = new Dictionary<string, string>();
        /// <summary>
        /// Allows custom request parameters to be added to API requests when using an API JSON data source.
        /// </summary>
        public Dictionary<string, string> ApiRequestParameters { get; set; } = new Dictionary<string, string>();
        /// <summary>
        /// Restricts the number of records returned from the data source query. Only valid for SQL based data sources.
        /// </summary>
        public int QueryLimit { get; set; } = -1;
        public bool DiagnosticsMode { get; set; } = false;
        public string ClassName { get; set; }
        internal string SearchDialogConjunction { get; set; } = "and";
        public string Message = string.Empty;
        [JsonProperty]
        internal MessageType MessageType = MessageType.None;
        internal bool IsParent => LinkedControlIds.Any();
        [JsonIgnore]
        public HttpContext HttpContext { get; internal set; } = null;
        [JsonIgnore]
        public IConfiguration Configuration => HttpContext?.RequestServices.GetService<IConfiguration>();

        public Type DataSourcePlugin
        {
            set { DataSourcePluginName = PluginHelper.GetNameFromType(value); }
        }
        public Type DataSourcePluginType
        {
            set { DataSourcePluginTypeName = PluginHelper.GetNameFromType(value); }
        }
        [JsonProperty]
        internal string DataSourcePluginName { get; set; } = string.Empty;
        [JsonProperty]
        internal string DataSourcePluginTypeName { get; set; } = string.Empty;
        public ComponentModel()
        {
            Id = GeneratedId();
        }
        public ComponentModel(DataSourceType dataSourceType, string url) : this()
        {
            DataSourceType = dataSourceType;
            Url = url;
        }

        public ComponentModel(Type dataSourcePlugin, Type dataSourcePluginType) : this()
        {
            DataSourceType = DataSourceType.IEnumerable;
            DataSourcePlugin = dataSourcePlugin;
            DataSourcePluginType = dataSourcePluginType;
        }

        public ComponentModel(DataSourceType dataSourceType, string connectionAlias, string tableName) : this()
        {
            DataSourceType = dataSourceType;
            ConnectionAlias = connectionAlias;
            TableName = tableName;
        }

        public ComponentModel(string tableName) : this()
        {
            TableName = tableName;
        }

        internal DataColumn GetDataColumn(ColumnModel column)
        {
            if (column == null)
            {
                return null;
            }
            return Data.Columns.Cast<DataColumn>().FirstOrDefault(c => c.ColumnName.ToLower() == column.Name.ToLower() || c.ColumnName.ToLower() == column.ColumnName.ToLower() || c.ColumnName.ToLower() == column.Expression.ToLower());
        }

        internal ColumnModel GetColumn(string columnName)
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
            return new List<object>() { RowId };// JsonConvert.DeserializeObject<List<object>>(TextHelper.DeobfuscateString(RowId,HttpContext)) ?? new List<object>();
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
        internal abstract ColumnModel SortColumn { get; }
        internal abstract SortOrder? SortSequence { get; set; }
      
    }
}