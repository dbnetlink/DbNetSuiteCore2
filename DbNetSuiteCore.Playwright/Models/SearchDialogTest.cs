using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.Playwright.Models
{
    public class SearchDialogTest
    {
        public string ColumnName { get; set; }
        public string FilterValue { get; set; }
        public int ExpectedRowCount {  get; set; }
        public SearchOperator? SearchOperator { get; set; } = null;
        public ResourceNames? ErrorString { get; set; } = null;
        public SearchDialogTest(string columnName, SearchOperator? searchOperator, string filterValue, int expectedRowCount)
        {
            ColumnName = columnName;
            FilterValue = filterValue;
            ExpectedRowCount = expectedRowCount;
            SearchOperator = searchOperator;
        }
    }
}
