using DbNetSuiteCore.Enums;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Data;

namespace DbNetSuiteCore.Models
{
    public class GridModel : ComponentModel
    {
        private string _SortKey = string.Empty;
        private SortOrder? _SortSequence = null;
        private RowSelection _RowSelection = RowSelection.Single;
        private string _Url = string.Empty;
        public string Id { get; set; } = string.Empty;
		[JsonIgnore]
		public IEnumerable<GridColumn> VisbleColumns => Columns.Where(c => c.DataOnly == false);
		[JsonIgnore]
		public IEnumerable<GridColumn> FilterColumns => Columns.Where(c => c.Filter != FilterType.None);
		[JsonIgnore]
		public IEnumerable<GridColumn> DataOnlyColumns => Columns.Where(c => c.DataOnly);
		[JsonIgnore]
		internal IEnumerable<GridColumn> ContentColumns => Columns.Where(c => c.Expression.StartsWith(FileSystemColumn.Content.ToString()) && string.IsNullOrEmpty(c.RegularExpression) == false);
        public int CurrentPage { get; set; } = 1;
        internal string SearchInput { get; set; } = string.Empty;
        internal string SortKey  
        { 
            get { return string.IsNullOrEmpty(_SortKey) ? InitalSortColumn?.Key ?? string.Empty : _SortKey; } 
            set { _SortKey = value; } 
        }
        public string CurrentSortKey { get; set; } = string.Empty;
        public SortOrder? CurrentSortSequence { get; set; }
        public bool CurrentSortAscending => SortSequence == SortOrder.Asc;
        internal string SortColumnName => SortColumn?.ColumnName ?? string.Empty;
        internal string SortColumnOrdinal => SortColumn?.Ordinal.ToString() ?? string.Empty;
        internal GridColumn? SortColumn => ((Columns.FirstOrDefault(c => c.Key == SortKey) ?? CurrentSortColumn) ?? InitalSortColumn);
        internal GridColumn? CurrentSortColumn => Columns.FirstOrDefault(c => c.Key == CurrentSortKey);
        internal GridColumn? InitalSortColumn => Columns.FirstOrDefault(c => c.InitialSortOrder.HasValue) ?? Columns.FirstOrDefault(c => c.Sortable);
        internal SortOrder? SortSequence 
        { 
            get { return _SortSequence == null ? (Columns.FirstOrDefault(c => c.InitialSortOrder.HasValue)?.InitialSortOrder ?? SortOrder.Asc) : _SortSequence; } 
            set { _SortSequence = value; } 
        }
        public string TableName { get; set; } = string.Empty;
        public string ProcedureName { get; set; } = string.Empty;
        public List<DbParameter> ProcedureParameters { get; set; } = new List<DbParameter>();
        internal string ExportFormat { get; set; } = string.Empty;
        public string ConnectionAlias { get; set; } = string.Empty;
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
        internal List<string> ColumnFilter { get; set; } = new List<string>();
        public int PageSize { get; set; } = 20;
        internal bool Uninitialised => Columns.Any() == false || Columns.Where(c => c.Initialised == false).Any();
        public bool IsNested { get; set; } = false;
        public bool IsLinked { get; set; } = false;
        public int ColSpan => VisbleColumns.ToList().Count;
        public string ParentKey { get; set; } = string.Empty;
        public DataSourceType DataSourceType { get; set; }
        public string HxFormTrigger => IsLinked ? "submit" : "load";
        public string TriggerName { get; set; } = string.Empty;
        public List<string> LinkedGridIds { get; set; } = new List<string>();
        public List<GridModel> _LinkedGrids { get; set; } = new List<GridModel>();
        public List<GridModel> _NestedGrids { get; set; } = new List<GridModel>();


        public IEnumerable<GridColumn> Columns { get; set; } = new List<GridColumn>();
		public string Caption { get; set; } = string.Empty;
        public Dictionary<GridClientEvent, string> ClientEvents { get; set; } = new Dictionary<GridClientEvent, string>();
        public string DatabaseName { get; set; } = string.Empty;
        public string FixedFilter { get; set; } = string.Empty;
        public List<DbParameter> FixedFilterParameters { get; set; } = new List<DbParameter>();
        [JsonIgnore]

        public GridModel NestedGrid
        {
            set
            {
                AddNestedGrid(value);
            }
        }
        public GridModel LinkedGrid
        {
            set
            {
                AddLinkedGrid(value);
            }
        }
        public ToolbarPosition ToolbarPosition { get; set; } = ToolbarPosition.Top;
        public RowSelection RowSelection { 
            get 
            { 
                return _RowSelection;
            } 
            set 
            {   
                _RowSelection = value;
                if (_RowSelection == RowSelection.Multiple)
                {
                    if (MultiRowSelectLocation == MultiRowSelectLocation.None)
                    {
                        MultiRowSelectLocation = MultiRowSelectLocation.Left;
                    }
                }
                else
                {
                    MultiRowSelectLocation = MultiRowSelectLocation.None;
                }
            } 
        }
        public MultiRowSelectLocation MultiRowSelectLocation { get; set; } = MultiRowSelectLocation.None;
        public HeadingMode HeadingMode { get; set; } = HeadingMode.Normal;
        public bool IsStoredProcedure { get; set; } = false;
        public ViewDialog? ViewDialog { get; set; }
        [Description("Boosts performance by caching data. Applied to Excel and JSON files only.")]
        public bool Cache { get; set; } = false;
        public int QueryLimit { get; set; } = -1;
        public bool DiagnosticsMode { get; set; } = false;
        public GridModel()
        {
            Id = GeneratedId();
        }
        public GridModel(DataSourceType dataSourceType, string url) :this()
        {
            DataSourceType = dataSourceType;
            Url = url;
        }

        public GridModel(DataSourceType dataSourceType, string connectionAlias, string tableName, bool isStoredProcedure = false) : this()
        {
            DataSourceType = dataSourceType;
            ConnectionAlias = connectionAlias;
            TableName = isStoredProcedure ? string.Empty : tableName;
            ProcedureName = isStoredProcedure ? tableName : string.Empty;
            IsStoredProcedure = isStoredProcedure;
        }

        public GridModel(DataSourceType dataSourceType, string connectionAlias, string procedureName, List<DbParameter> procedureParameters) : this()
        {
            DataSourceType = dataSourceType;
            ConnectionAlias = connectionAlias;
            ProcedureName = procedureName;
            ProcedureParameters = procedureParameters;
            IsStoredProcedure = true;
        }

        public GridModel(string tableName) : this()
        {
            TableName = tableName;
        }

        private string GeneratedId()
        {
            return $"Grid{DateTime.Now.Ticks}";
        }

        public void SetId()
        {
            Id = GeneratedId();
        }

        public void AddLinkedGrid(GridModel gridModel)
        {
            _LinkedGrids.Add(gridModel);
            LinkedGridIds.Add(gridModel.Id);
            gridModel.IsLinked = true;

            AssignLinkedProperties(this, gridModel);

            foreach (GridModel linkedGrid in gridModel._LinkedGrids)
            {
                AssignLinkedProperties(gridModel, linkedGrid);
            }
        }

        public void AddNestedGrid(GridModel gridModel)
        {
            _NestedGrids.Add(gridModel);
            gridModel.IsNested = true;

            AssignLinkedProperties(this, gridModel);

            foreach (GridModel nestedGrid in gridModel._NestedGrids)
            {
                AssignLinkedProperties(gridModel, nestedGrid);
            }
        }

        internal void AssignLinkedProperties(GridModel parent, GridModel child)
        {
            if (string.IsNullOrEmpty(child.ConnectionAlias))
            {
                child.ConnectionAlias = parent.ConnectionAlias;
                child.DataSourceType = parent.DataSourceType;
            }
        }

        public DataColumn? GetDataColumn(GridColumn column)
        {
            return Data.Columns.Cast<DataColumn>().FirstOrDefault(c => c.ColumnName.ToLower() == column.Name.ToLower() || c.ColumnName.ToLower() == column.ColumnName.ToLower() || c.ColumnName.ToLower() == column.Expression.ToLower());
        }

        public void ConfigureSort(string sortKey)
        {
            if (string.IsNullOrEmpty(sortKey) == false)
            {
                if (sortKey != CurrentSortKey)
                {
                    SortSequence = SortOrder.Asc;
                    CurrentSortKey = sortKey;
                }
                else
                {
                    SortSequence = (CurrentSortSequence ?? SortOrder.Asc) == SortOrder.Asc ? SortOrder.Desc : SortOrder.Asc;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(CurrentSortKey))
                {
                    CurrentSortKey = InitalSortColumn?.Key ?? string.Empty;
                }
            }

            CurrentSortSequence = SortSequence;
        }

        public string? PrimaryKeyValue(DataRow dataRow)
        {
            if (DataSourceType == DataSourceType.FileSystem)
            {
                return Convert.ToString(RowValue(dataRow, "Name", false));
            }
            else
            {
                var primaryKeyColumn = Columns.FirstOrDefault(c => c.PrimaryKey);
                if (primaryKeyColumn != null)
                {
                    var dataColumn = dataRow.Table.Columns.Cast<DataColumn>().ToList().FirstOrDefault(c => c.ColumnName == primaryKeyColumn.Name || primaryKeyColumn.Name.Split(".").Last() == c.ColumnName);

                    if (dataColumn != null)
                    {
                        return dataRow[dataColumn].ToString();
                    }
                }

                return null;
            }
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

        public void Bind(GridClientEvent clientEvent, string functionName)
        {
            ClientEvents[clientEvent] = functionName;
        }
    }
}