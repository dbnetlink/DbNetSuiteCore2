using DbNetSuiteCore.Enums;

namespace DbNetSuiteCore.Models
{
    public class SearchDialogFilter
    {
        public string ColumnKey { get; set; } = string.Empty;
        public SearchOperator Operator { get; set; }
        public object? Value1 { get; set; } = null;
        public object? Value2 { get; set; } = null;
    }
}
