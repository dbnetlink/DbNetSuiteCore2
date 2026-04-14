using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.Playwright.Models
{
    public enum FilterControl
    {
        Input,
        Select
    }
    public class ColumnFilterTest
    {
        public string ColumnName { get; set; }
        public string FilterValue { get; set; }
        public int ExpectedRowCount {  get; set; }
        public FilterControl FilterType { get; set; } = FilterControl.Input;
        public ResourceNames? ErrorString { get; set; } = null;
        public ColumnFilterTest(string columnName, string filterValue, int expectedRowCount, FilterControl filterType = FilterControl.Input)
        {
            ColumnName = columnName;
            FilterValue = filterValue;
            ExpectedRowCount = expectedRowCount;
            FilterType = filterType;
        }

        public ColumnFilterTest(string columnName, string filterValue, ResourceNames errorString, FilterControl filterType = FilterControl.Input)
        {
            ColumnName = columnName;
            FilterValue = filterValue;
            ErrorString = errorString;
            FilterType = filterType;
        }
    }
}
