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
        public ColumnFilterTest(string columnName, string filterValue, int expectedRowCount, FilterType filterType = FilterType.Input)
        {
            ColumnName = columnName;
            FilterValue = filterValue;
            ExpectedRowCount = expectedRowCount;
            FilterType = filterType;
        }
    }
}
