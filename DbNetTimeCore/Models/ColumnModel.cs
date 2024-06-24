namespace DbNetTimeCore.Models
{
    public class ColumnModel
    {
        public string Label { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ColumnName => Name.Split(".").Last();
        public string Key => Name.GetHashCode().ToString();
        public bool IsPrimaryKey { get; set; } = false;
        public Type DataType { get; set; } = typeof(String);
        public string Format { get; set; } = string.Empty;

        public ColumnModel()
        {
        }
        public ColumnModel(string name, string label)
        {
            Name = name;
            Label = label;
        }
    }
}
