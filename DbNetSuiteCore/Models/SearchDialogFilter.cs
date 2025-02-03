using DbNetSuiteCore.Enums;

namespace DbNetSuiteCore.Models
{
    public class SearchDialogFilter
    {
        public string ColumnKey { get; set; } = string.Empty;
        public SearchOperator Operator { get; set; }
        public string Value1 { get; set; } = string.Empty;
        public string Value2 { get; set; } = string.Empty;
    }
}
