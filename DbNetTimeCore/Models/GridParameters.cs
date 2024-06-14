namespace DbNetTimeCore.Models
{
    public class GridParameters
    {
        public int CurrentPage { get; set; } = 1;
        public string SearchInput { get; set; } = string.Empty;
        public string SortKey { get; set; } = string.Empty;
        public List<ColumnInfo> Columns { get; set; } = new List<ColumnInfo>();
        public string CurrentSortKey { get; set; } = string.Empty;
        public bool CurrentSortAscending { get; set; } = true;
        public string SortColumn => Columns.FirstOrDefault(c => c.Key == SortKey)?.Name ?? "1";
        public string CurrentSortColumn => Columns.FirstOrDefault(c => c.Key == CurrentSortKey)?.Name ?? "1";
        public string SortSequence => GetSortSequence();
        public string Handler { get; set; } = string.Empty;
        public string? PrimaryKey { get; set; }
        public int ColSpan { get; set; }
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