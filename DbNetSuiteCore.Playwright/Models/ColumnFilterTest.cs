using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.Playwright.Models
{
    public enum FilterType
    {
        Input,
        Select
    }
    public class ColumnFilterTest
    {
        public string ColumnName { get; set; }
        public string FilterValue { get; set; }
        public int ExpectedRowCount {  get; set; }
        public FilterType FilterType { get; set; } = FilterType.Input;
        public ResourceNames? ErrorString { get; set; } = null;
        public ColumnFilterTest(string columnName, string filterValue, int expectedRowCount, FilterType filterType = FilterType.Input)
        {
            ColumnName = columnName;
            FilterValue = filterValue;
            ExpectedRowCount = expectedRowCount;
            FilterType = filterType;
        }

        public ColumnFilterTest(string columnName, string filterValue, ResourceNames errorString, FilterType filterType = FilterType.Input)
        {
            ColumnName = columnName;
            FilterValue = filterValue;
            ErrorString = errorString;
            FilterType = filterType;
        }
    }
}
