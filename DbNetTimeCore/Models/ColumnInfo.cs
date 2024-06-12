namespace DbNetTimeCore.Models
{
    public class ColumnInfo
    {
        public string Label { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Searchable { get; set; } = false;
        public string Format { get; set; } = string.Empty;
        public bool IsPrimaryKey { get; set; } = false;
        public bool Editable { get; set; } = false;
        public Type DataType { get; set; } = typeof(String);
        public bool SortedBy { get; set; } = false;
        public bool SortedByAscending { get; set; } = true;

        public ColumnInfo()
        {
        }
        public ColumnInfo(string name, string label, bool searchable = false)
        {
            Name = name;
            Label = label;
            Searchable = searchable;
        }
    }
}