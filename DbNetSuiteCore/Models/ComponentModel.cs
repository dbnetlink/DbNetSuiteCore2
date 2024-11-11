using System.Text.Json.Serialization;
using System.Data;
using DbNetSuiteCore.Enums;

namespace DbNetSuiteCore.Models
{
    public abstract class ComponentModel
    {
        protected RowSelection _RowSelection = RowSelection.Single;
        private string _Url = string.Empty;
        public string Id { get; set; } = string.Empty;
        public DataSourceType DataSourceType { get; set; }
        [JsonIgnore]
        public DataTable Data { get; set; } = new DataTable();
        public string TableName { get; set; } = string.Empty;
        public string ProcedureName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public List<DbParameter> ProcedureParameters { get; set; } = new List<DbParameter>();
        public string ConnectionAlias { get; set; } = string.Empty;
        public bool IsStoredProcedure { get; set; } = false;
        public Dictionary<string, List<string>> LinkedControlIds { get; set; } = new Dictionary<string, List<string>>();
        internal bool Uninitialised => GetColumns().Any() == false || GetColumns().Where(c => c.Initialised == false).Any();
        internal string SearchInput { get; set; } = string.Empty;
        internal string SortColumnName => SortColumn?.ColumnName ?? string.Empty;
        internal string SortColumnOrdinal => SortColumn?.Ordinal.ToString() ?? string.Empty;
        public bool Distinct { get; set; } = false;
        public string Caption { get; set; } = string.Empty;

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
        public string JSON { get; set; } = string.Empty;
        private List<ComponentModel> _LinkedControls { get; set; } = new List<ComponentModel>();

        public ComponentModel LinkedControl
        {
            set
            {
                AddLinkedControl(value);
            }
        }
        public bool IsLinked { get; set; } = false;
        public string ParentKey { get; set; } = string.Empty;
        public string HxFormTrigger => IsLinked ? "submit" : "load";
        public string PostUrl => $"{this.GetType().Name.Replace("Model", "control").ToLower()}.htmx";
        public string TriggerName { get; set; } = string.Empty;
        public string FixedFilter { get; set; } = string.Empty;
        public List<DbParameter> FixedFilterParameters { get; set; } = new List<DbParameter>();
        public bool Cache { get; set; } = false;
        public int QueryLimit { get; set; } = -1;
        public bool DiagnosticsMode { get; set; } = false;


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

        public DataColumn? GetDataColumn(ColumnModel column)
        {
            return Data.Columns.Cast<DataColumn>().FirstOrDefault(c => c.ColumnName.ToLower() == column.Name.ToLower() || c.ColumnName.ToLower() == column.ColumnName.ToLower() || c.ColumnName.ToLower() == column.Expression.ToLower());
        }

        public object RowValue(DataRow dataRow, string columnName, object defaultValue)
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

        public void SetId()
        {
            Id = GeneratedId();
        }

        public List<string> GetLinkedControlIds(string typeName)
        {
            return LinkedControlIds.ContainsKey(typeName) ? LinkedControlIds[typeName] : new List<string>();
        }


        public List<string> GetLinkedControlIds()
        {
            return LinkedControlIds.SelectMany(d => d.Value).ToList();
        }

        public void AddLinkedControl(ComponentModel componentModel)
        {
            _LinkedControls.Add(componentModel);
            componentModel.IsLinked = true;

            var typeName = componentModel.GetType().Name;

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

        public abstract IEnumerable<ColumnModel> GetColumns();
        public abstract void SetColumns(IEnumerable<ColumnModel> columns);
        public abstract ColumnModel NewColumn(DataRow dataRow);
        public abstract ColumnModel NewColumn(DataColumn dataColumn, DataSourceType dataSourceType);
        internal abstract ColumnModel? SortColumn { get; }
        internal abstract SortOrder? SortSequence { get; set; }
    }
}