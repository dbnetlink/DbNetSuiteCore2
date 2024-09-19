using DbNetSuiteCore.Enums;
using System.Collections.Specialized;
using System.Data;

namespace DbNetSuiteCore.Models
{
    public class GridModel : ComponentModel
    {
        private string _SortKey = string.Empty;
        private SortOrder? _SortSequence = null;
        private GridModel? _LinkedGrid;
        public string Id { get; set; } = string.Empty;
        public IEnumerable<GridColumnModel> Columns { get; set; } = new List<GridColumnModel>();
        public IEnumerable<GridColumnModel> VisbleColumns => Columns.Where(c => c.DataOnly == false);
        public IEnumerable<GridColumnModel> DataOnlyColumns => Columns.Where(c => c.DataOnly);
        public int CurrentPage { get; set; } = 1;
        public string SearchInput { get; set; } = string.Empty;
        public string SortKey  
        { 
            get { return string.IsNullOrEmpty(_SortKey) ? Columns.FirstOrDefault()?.Key ?? string.Empty : _SortKey; } 
            set { _SortKey = value; } 
        }
        public string CurrentSortKey { get; set; } = string.Empty;
        public bool CurrentSortAscending => SortSequence == SortOrder.Asc;
        public string SortColumnName => SortColumn?.ColumnName ?? string.Empty;
        public string SortColumnOrdinal => SortColumn?.Ordinal.ToString() ?? string.Empty;
        public GridColumnModel? SortColumn => ((Columns.FirstOrDefault(c => c.Key == SortKey) ?? CurrentSortColumn) ?? InitalSortColumn);
        public GridColumnModel? CurrentSortColumn => Columns.FirstOrDefault(c => c.Key == CurrentSortKey);
        public GridColumnModel? InitalSortColumn => Columns.FirstOrDefault(c => c.InitialSortOrder.HasValue) ?? Columns.FirstOrDefault(c => c.Sortable);
        public SortOrder? SortSequence 
        { 
            get { return _SortSequence == null ? (Columns.FirstOrDefault(c => c.InitialSortOrder.HasValue)?.InitialSortOrder ?? SortOrder.Asc) : _SortSequence; } 
            set { _SortSequence = value; } 
        }
        public string? PrimaryKey { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string ProcedureName { get; set; } = string.Empty;
        public List<ProcedureParameter> ProcedureParameters { get; set; } = new List<ProcedureParameter>();
        public string ExportFormat { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty;
        public string ConnectionAlias { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string FixedFilter { get; set; } = string.Empty;
        public List<string> ColumnFilter { get; set; } = new List<string>();
        public int PageSize { get; set; } = 20;
        public bool Uninitialised => Columns.Any() == false || Columns.Where(c => c.Initialised == false).Any();
        public GridModel? NestedGrid { get; set; } = null;
        public GridModel? LinkedGrid
        {
            get { return _LinkedGrid; }
            set 
            { 
                _LinkedGrid = value; 
                if (value != null)
                {
                    value.IsLinked = true;
                }
            }
        }
        public bool IsNested { get; set; } = false;
        public bool IsLinked { get; set; } = false;
        public int ColSpan => VisbleColumns.ToList().Count;
        public string ParentKey { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public Dictionary<ClientEvent,string> ClientEvents { get; set; } = new Dictionary<ClientEvent, string>();
        public DataSourceType DataSourceType { get; set; }
        public string HxFormTrigger => IsLinked ? "submit" : "load";
        public string TriggerName { get; set; } = string.Empty;
        public ToolbarPosition ToolbarPosition { get; set; } = ToolbarPosition.Top;
        public MultiRowSelectLocation MultiRowSelect { get; set; } = MultiRowSelectLocation.None;
        public bool FrozenHeader { get; set; } = false;
        public bool IsStoredProcedure { get; set; } = false;
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

        public GridModel(DataSourceType dataSourceType, string connectionAlias, string procedureName, List<ProcedureParameter> procedureParameters) : this()
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
            _LinkedGrid = gridModel;
            gridModel.IsLinked = true;

            var linkedGrid = gridModel;

            while (linkedGrid != null)
            {
                if (string.IsNullOrEmpty(ConnectionAlias) == false)
                {
                    linkedGrid.ConnectionAlias = ConnectionAlias;
                    linkedGrid.DataSourceType = DataSourceType;
                }
                
                linkedGrid = linkedGrid.LinkedGrid;
            }
        }

        public DataColumn? GetDataColumn(GridColumnModel column)
        {
            return Data.Columns.Cast<DataColumn>().FirstOrDefault(c => c.ColumnName.ToLower() == column.Name.ToLower() || c.ColumnName.ToLower() == column.ColumnName.ToLower());
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
                    SortSequence = SortSequence == SortOrder.Asc ? SortOrder.Desc : SortOrder.Asc;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(CurrentSortKey))
                {
                    CurrentSortKey = InitalSortColumn?.Key ?? string.Empty;
                }
            }
        }
    }
}