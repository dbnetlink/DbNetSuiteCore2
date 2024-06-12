namespace DbNetTimeCore.Models
{
    public class GridParameters
    {
        public int CurrentPage { get; set; } = 1;
        public string SearchInput { get; set; } = string.Empty;

        public List<ColumnInfo> Columns { get; set; } = new List<ColumnInfo>();
    }
}
