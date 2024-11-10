using DbNetSuiteCore.Enums;
using System.Text.Json.Serialization;
using System.Data;

namespace DbNetSuiteCore.Models
{
    public class GridModel : ComponentModel
    {
        private string _SortKey = string.Empty;
        private SortOrder? _SortSequence = null;
		[JsonIgnore]
		public IEnumerable<GridColumn> VisbleColumns => Columns.Where(c => c.DataOnly == false);
		[JsonIgnore]
		public IEnumerable<GridColumn> FilterColumns => Columns.Where(c => c.Filter != FilterType.None);
		[JsonIgnore]
		public IEnumerable<GridColumn> DataOnlyColumns => Columns.Where(c => c.DataOnly);
		[JsonIgnore]
		internal IEnumerable<GridColumn> ContentColumns => Columns.Where(c => c.Expression.StartsWith(FileSystemColumn.Content.ToString()) && string.IsNullOrEmpty(c.RegularExpression) == false);
        public int CurrentPage { get; set; } = 1;
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
        internal GridColumn? SortColumn => (Columns.FirstOrDefault(c => c.Key == SortKey) ?? CurrentSortColumn) ?? InitalSortColumn;
        internal GridColumn? CurrentSortColumn => Columns.FirstOrDefault(c => c.Key == CurrentSortKey);
        internal GridColumn? InitalSortColumn => Columns.FirstOrDefault(c => c.InitialSortOrder.HasValue) ?? Columns.FirstOrDefault(c => c.Sortable);
        internal SortOrder? SortSequence 
        { 
            get { return _SortSequence == null ? (Columns.FirstOrDefault(c => c.InitialSortOrder.HasValue)?.InitialSortOrder ?? SortOrder.Asc) : _SortSequence; } 
            set { _SortSequence = value; } 
        }
        internal string ExportFormat { get; set; } = string.Empty;
        internal List<string> ColumnFilter { get; set; } = new List<string>();
        public int PageSize { get; set; } = 20;
        public bool IsNested { get; set; } = false;
       
        public int ColSpan => VisbleColumns.ToList().Count;

        public List<string> LinkedGridIds => GetLinkedControlIds(nameof(GridModel));  

        public List<GridModel> _NestedGrids { get; set; } = new List<GridModel>();

        public IEnumerable<GridColumn> Columns { get; set; } = new List<GridColumn>();
		public string Caption { get; set; } = string.Empty;
        public Dictionary<GridClientEvent, string> ClientEvents { get; set; } = new Dictionary<GridClientEvent, string>();

        [JsonIgnore]

        public GridModel NestedGrid
        {
            set
            {
                AddNestedGrid(value);
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
        public ViewDialog? ViewDialog { get; set; }
     
        public GridModel() : base()
        {
        }
        public GridModel(DataSourceType dataSourceType, string url) :base(dataSourceType,url)
        {
        }

        public GridModel(DataSourceType dataSourceType, string connectionAlias, string tableName, bool isStoredProcedure = false) : base(dataSourceType, connectionAlias, tableName, isStoredProcedure)
        {
        }

        public GridModel(DataSourceType dataSourceType, string connectionAlias, string procedureName, List<DbParameter> procedureParameters) : base(dataSourceType, connectionAlias, procedureName, procedureParameters)
        {
        }

        public GridModel(string tableName) : base(tableName)
        {
        }

        public override IEnumerable<ColumnModel> GetColumns()
        {
            return Columns.Cast<ColumnModel>();
        }

        public override void SetColumns(IEnumerable<ColumnModel> columns)
        {
            Columns = columns.Cast<GridColumn>();
        }

        public override ColumnModel NewColumn(DataRow dataRow)
        { 
            return new GridColumn(dataRow); 
        }
        public override ColumnModel NewColumn(DataColumn dataColumn, DataSourceType dataSourceType)
        {
            return new GridColumn(dataColumn, dataSourceType);
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


        public void Bind(GridClientEvent clientEvent, string functionName)
        {
            ClientEvents[clientEvent] = functionName;
        }
    }
}