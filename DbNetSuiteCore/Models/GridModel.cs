using DbNetSuiteCore.Enums;
using System.Data;

namespace DbNetSuiteCore.Models
{
    public class GridModel : ComponentModel
    {
        private GridModel? _LinkedGrid;
        public string Id { get; set; } = string.Empty;
        public List<GridColumnModel> GridColumns => Columns.Cast<GridColumnModel>().ToList();
        public int CurrentPage { get; set; } = 1;
        public string SearchInput { get; set; } = string.Empty;
        public string SortKey { get; set; } = string.Empty;
        public string CurrentSortKey { get; set; } = string.Empty;
        public bool CurrentSortAscending { get; set; } = true;
        public string SortColumn => Columns.FirstOrDefault(c => c.Key == SortKey)?.Ordinal.ToString() ?? "1";
        public string CurrentSortColumn => Columns.FirstOrDefault(c => c.Key == CurrentSortKey)?.Ordinal.ToString() ?? "1";
        public SortOrder SortSequence => GetSortSequence();
        public string? PrimaryKey { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string ExportFormat { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty;
        public string ConnectionAlias { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string FixedFilter { get; set; } = string.Empty;
        public List<string> ColumnFilter { get; set; } = new List<string>();
        public int PageSize { get; set; } = 20;
        public bool Uninitialised => GridColumns.Any() == false || GridColumns.Where(c => c.Initialised == false).Any();
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
        public int ColSpan { get; set; } = 0;
        public string ParentKey { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public Dictionary<ClientEvent,string> ClientEvents { get; set; } = new Dictionary<ClientEvent, string>();
        public DataSourceType DataSourceType { get; set; }
        private SortOrder GetSortSequence()
        {
            if (string.IsNullOrEmpty(SortKey))
            {
                return CurrentSortAscending ? SortOrder.Asc : SortOrder.Desc;
            }
            if (SortKey == CurrentSortKey)
            {
                return CurrentSortAscending ? SortOrder.Desc : SortOrder.Asc;
            }

            return SortOrder.Asc;
        }
        public string HxFormTrigger => IsLinked ? "submit" : "load";
        public ToolbarPosition ToolbarPosition { get; set; } = ToolbarPosition.Top;

        public GridModel()
        {
            Id = GeneratedId();
        }

        public GridModel(DataSourceType dataSourceType, string url) :this()
        {
            DataSourceType = dataSourceType;
            Url = url;
        }

        public GridModel(DataSourceType dataSourceType, string connectionAlias, string tableName) : this()
        {
            DataSourceType = dataSourceType;
            ConnectionAlias = connectionAlias;
            TableName = tableName;
        }

        private string GeneratedId()
        {
            return $"Grid{DateTime.Now.Ticks}";
        }

        public void SetId()
        {
            Id = $"Grid{DateTime.Now.Ticks}";
        }
    }
}