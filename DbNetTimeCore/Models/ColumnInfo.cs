using DbNetTimeCore.Enums;
using System.Data;
using static DbNetTimeCore.Utilities.DbNetDataCore;

namespace DbNetTimeCore.Models
{
    public class ColumnInfo
    {
        public string Label { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ColumnName => Name.Split(".").Last();
        public string Key => Name.GetHashCode().ToString();
        public bool Searchable { get; set; } = false;
        public string Format { get; set; } = string.Empty;
        public bool IsPrimaryKey { get; set; } = false;
        public bool Editable { get; set; } = false;
        public Type DataType { get; set; } = typeof(String);
        public string ClassName { get; set; } = "w-full";
        public QueryCommandConfig? Lookup { get; set; }
        public Type? LookupEnum { get; set; }
        public DataTable LookupValues { get; set; } = new DataTable();
        public EditControlType? EditControlType { get; set; }
        public int? MaxTextLength { get; set; }

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
