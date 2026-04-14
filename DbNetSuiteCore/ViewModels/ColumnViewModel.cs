using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Html;

namespace DbNetSuiteCore.ViewModels
{
    public class ColumnViewModel
    {
        private ColumnModel Column { get; set; }
        public string ColumnName => Column.ColumnName;
        public string Label => Column.Label;

        public bool IsNumeric => Column.IsNumeric;
        public List<KeyValuePair<string, string>> EnumOptions => Column.EnumOptions;
        public List<KeyValuePair<string, string>> LookupOptions => Column.LookupOptions;

        public string DataTypeName => Column.DataTypeName;
        public string DbDataType => Column.DbDataType;
        public string UserDataType => Column.UserDataType;
        public string Key => Column.Key;

        public string ToStringOrEmpty(object value)
        {
            return Column.ToStringOrEmpty(value);
        }
        public ColumnViewModel(ColumnModel column)
        {
            Column = column;
        }

        public HtmlString SearchOperatorSelection(DataSourceType dataSourceType)
        {
            return Column.SearchOperatorSelection(dataSourceType);
        }

        public HtmlString SearchInput()
        {
            return Column.SearchInput();
        }
    }
}
