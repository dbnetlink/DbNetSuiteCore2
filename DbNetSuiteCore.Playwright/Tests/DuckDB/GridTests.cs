using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Playwright.Models;
using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests.DuckDB
{
    public class GridTests : DuckDbSetUp
    {
        [Test]
        public async Task QuickSearchTest()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "loop", 7 },
                { string.Empty, 412 },
                { "Ordynacka 10", 7 }
            };

            await GridQuickSearchTest(searches, $"duckdb/invoices");
        }

        [Test]
        public async Task HeadingSort()
        {
            Dictionary<string, string> sorts = new Dictionary<string, string>() {
                { "customer_id", "1"},
                { "first_name","AARON" },
                { "last_name", "ABNEY" },
                { "email", "AARON.SELBY@sakilacustomer.org" },
                { "city", "A Corua (La Corua)" },
                { "postal_code", "3" },
                { "create_date", "14/02/2006" },
                { "last_update", "15/02/06" }
            };

            await GridHeadingSort(sorts, $"duckdb/joined");
        }

        [Test]
        public async Task HeadingReverseSort()
        {
            Dictionary<string, KeyValuePair<string, string>> sorts = new Dictionary<string, KeyValuePair<string, string>>() {
                { "customer_id", new KeyValuePair<string, string>("1","599")  },
                { "first_name", new KeyValuePair<string, string>("AARON","ZACHARY") },
                { "last_name", new KeyValuePair<string, string>("ABNEY","YOUNG") },
                { "email", new KeyValuePair<string, string>("AARON.SELBY@sakilacustomer.org","ZACHARY.HITE@sakilacustomer.org") },
                { "city", new KeyValuePair<string, string>("A Corua (La Corua)","s-Hertogenbosch") },
                { "address", new KeyValuePair<string, string>("1 Valle de Santiago Avenue","999 Sanaa Loop") },
                { "postal_code", new KeyValuePair<string, string>("3","99865") },
                { "create_date", new KeyValuePair<string, string>("14/02/2006","14/02/2006") },
                { "last_update", new KeyValuePair<string, string>("15/02/06","15/02/06") }
            };

            await GridHeadingReverseSort(sorts, "duckdb/joined");
        }

        [Test]
        public async Task ColumnFilter()
        {
            List<ColumnFilterTest> filterTests = new List<ColumnFilterTest>() {
                new ColumnFilterTest("create_date","<14/02/2006",0),
                new ColumnFilterTest("create_date",">14/02/2006",0),
                new ColumnFilterTest("create_date",">=14/02/2006",599),
                new ColumnFilterTest("create_date","14/02/2006",599),
                new ColumnFilterTest("create_date","",599),
                new ColumnFilterTest("active","0",15, FilterControl.Select),
                new ColumnFilterTest("active","1",584, FilterControl.Select),
                new ColumnFilterTest("postal_code","00",15),
                new ColumnFilterTest("active","1",15, FilterControl.Select),
                new ColumnFilterTest("create_date","xxx",Helpers.ResourceNames.DataFormatError),
                new ColumnFilterTest("create_date","",15),
                new ColumnFilterTest("create_date",">=",Helpers.ResourceNames.ColumnFilterNoData)
            };

            await GridColumnFilter(filterTests, "duckdb/joined");
        }

        [Test]
        public async Task ColumnFilterInitialValue()
        {
            List<ColumnFilterInitialValueTest> filterTests = new List<ColumnFilterInitialValueTest>() {
                new ColumnFilterInitialValueTest { ColumnFilter = new Dictionary<string, string> { { "create_date", "14/02/2006" } }, ExpectedRowCount = 599 },
                new ColumnFilterInitialValueTest { ColumnFilter = new Dictionary<string, string> { { "active", "no" } }, ExpectedRowCount = 15 },
                new ColumnFilterInitialValueTest { ColumnFilter = new Dictionary<string, string> { { "active", "yes" } }, ExpectedRowCount = 584 },
                new ColumnFilterInitialValueTest { ColumnFilter = new Dictionary<string, string> { { "city", "London" } }, ExpectedRowCount = 2 },
                new ColumnFilterInitialValueTest { ColumnFilter = new Dictionary<string, string> { { "postal_code", "00" } }, ExpectedRowCount = 15 },
                new ColumnFilterInitialValueTest { ColumnFilter = new Dictionary<string, string> { { "postal_code", "00" },{ "active", "yes" } }, ExpectedRowCount = 15 },
            };

            await GridColumnFilterInitialValue(filterTests, "duckdb/joined");
        }

        [Test]
        public async Task SearchDialog()
        {
            List<SearchDialogTest> searchDialogTests = new List<SearchDialogTest>() {/*
                new SearchDialogTest("create_date",SearchOperator.GreaterThan,"14/02/2006",0),
                new SearchDialogTest("create_date",SearchOperator.NotLessThan,"14/02/2006",599),
                new SearchDialogTest("create_date",SearchOperator.NotGreaterThan, "14/02/2006",599),
                new SearchDialogTest("create_date",SearchOperator.EqualTo,"14/02/2006",599),
                new SearchDialogTest("create_date",null,string.Empty,599),
                */
                new SearchDialogTest("active",SearchOperator.False,string.Empty,15),
                new SearchDialogTest("active",SearchOperator.True, string.Empty,584),
             //   new SearchDialogTest("postal_code",SearchOperator.Contains,"00",15)
            };

            await GridSearchDialogFilter(searchDialogTests, "duckdb/joined");
        }
    }
}