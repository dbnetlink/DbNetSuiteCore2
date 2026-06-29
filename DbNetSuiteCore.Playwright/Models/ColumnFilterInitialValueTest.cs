namespace DbNetSuiteCore.Playwright.Models
{

    public class ColumnFilterInitialValueTest
    {
        public Dictionary<string, string> ColumnFilter { get; set; } = new Dictionary<string, string>();
        public int ExpectedRowCount {  get; set; }
    }
}
