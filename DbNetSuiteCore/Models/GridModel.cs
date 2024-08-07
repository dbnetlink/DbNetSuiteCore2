using DbNetTimeCore.Enums;

namespace TQ.Models
{
    public class GridModel : ComponentModel
    {
        public string Id { get; set; } = string.Empty;
        public string IndicatorId => $"{Id}Indicator";
        public string ContainerId => $"{Id}Container";
        public List<GridColumnModel> GridColumns => Columns.Cast<GridColumnModel>().ToList();
        public int CurrentPage { get; set; } = 1;
        public string SearchInput { get; set; } = string.Empty;
        public string SortKey { get; set; } = string.Empty;
        public string CurrentSortKey { get; set; } = string.Empty;
        public bool CurrentSortAscending { get; set; } = true;
        public string SortColumn => Columns.FirstOrDefault(c => c.Key == SortKey)?.Ordinal.ToString() ?? "1";
        public string CurrentSortColumn => Columns.FirstOrDefault(c => c.Key == CurrentSortKey)?.Ordinal.ToString() ?? "1";
        public string SortSequence => GetSortSequence();
        public string? PrimaryKey { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string ExportFormat { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty;
        public string ConnectionAlias { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string FixedFilter { get; set; } = string.Empty;
        public int PageSize { get; set; } = 20;
        public DataSourceType DataSourceType { get; set; }
        private string GetSortSequence()
        {
            if (string.IsNullOrEmpty(SortKey))
            {
                return CurrentSortAscending ? "asc" : "desc";
            }
            if (SortKey == CurrentSortKey)
            {
                return CurrentSortAscending ? "desc" : "asc";
            }

            return "asc";
        }

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

        public string GeneratedId()
        {
            return $"Grid{DateTime.Now.Ticks}";
        }
    }
}