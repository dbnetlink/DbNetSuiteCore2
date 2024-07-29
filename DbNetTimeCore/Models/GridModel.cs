using DbNetTimeCore.Enums;

namespace DbNetTimeCore.Models
{
    public class GridModel : ComponentModel
    {
        public string Id => $"{new string(TableName.Split(" ").First().Where(c => char.IsLetterOrDigit(c)).ToArray())}Grid";
        public List<GridColumnModel> GridColumns => Columns.Cast<GridColumnModel>().ToList();
        public int CurrentPage { get; set; } = 1;
        public string SearchInput { get; set; } = string.Empty;
        public string SortKey { get; set; } = string.Empty;
        public string CurrentSortKey { get; set; } = string.Empty;
        public bool CurrentSortAscending { get; set; } = true;
        public string SortColumn => Columns.FirstOrDefault(c => c.Key == SortKey)?.Ordinal.ToString() ?? "1";
        public string CurrentSortColumn => Columns.FirstOrDefault(c => c.Key == CurrentSortKey)? .Ordinal.ToString() ?? "1";
        public string SortSequence => GetSortSequence();
        public string? PrimaryKey { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string ConnectionAlias { get; set; } = string.Empty;
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
    }
}