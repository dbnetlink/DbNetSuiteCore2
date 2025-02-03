using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Playwright.Models;
using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests.PostgreSql
{
    [TestFixture]
    public class GridTests : PostgreSqlDbSetUp
    {
        [Test]
        public async Task QuickSearchTest()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "germany", 11 },
                { "67", 60 },
                { string.Empty, 91}
            };

            await GridQuickSearchTest(searches, $"postgresql/customers?db={DatabaseName}");
        }

        [Test]
        public async Task HeadingSort()
        {
            Dictionary<string, string> sorts = new Dictionary<string, string>()
            {
                { "Custid", "91"},
                { "CompanyName","Customer AHPOP" },
                { "ContactName", "Allen, Michael" },
                { "ContactTitle", "Accounting Manager" },
                { "Address", "0123 Grizzly Peak Rd." },
                { "City", "Aachen" },
                { "Region", "AK" },
                { "PostalCode", "10038" },
                { "Country", "Argentina" },
                { "Phone", "(02) 890 12 34" },
                { "Fax", "(02) 567 89 01" }
            };

            await GridHeadingSort(sorts, $"postgresql/customers?db={DatabaseName}");
        }

        [Test]
        public async Task HeadingReverseSort()
        {
            Dictionary<string, KeyValuePair<string, string>> sorts = new Dictionary<string, KeyValuePair<string, string>>()
            {
                { "Custid", new KeyValuePair<string, string>("91","1")},
                { "CompanyName", new KeyValuePair<string, string>("Customer AHPOP","Customer ZRNDE") },
                { "ContactName", new KeyValuePair<string, string>("Allen, Michael","Young, Robin") },
                { "ContactTitle", new KeyValuePair<string, string>("Accounting Manager","Sales Representative") },
                { "Address", new KeyValuePair<string, string>("0123 Grizzly Peak Rd.","Walserweg 4567") },
                { "City", new KeyValuePair<string, string>("Aachen","Warszawa") },
                { "Region", new KeyValuePair<string, string>("AK",string.Empty) },
                { "PostalCode", new KeyValuePair<string, string>("10038","10128") },
                { "Country", new KeyValuePair<string, string>("Argentina","Venezuela") },
                { "Phone", new KeyValuePair<string, string>("(02) 890 12 34","981-123456") },
                { "Fax", new KeyValuePair<string, string>("(02) 567 89 01"," ") }
            };

            await GridHeadingReverseSort(sorts, $"postgresql/customers?db={DatabaseName}");
        }

        [Test]
        public async Task ColumnFilter()
        {
            List<ColumnFilterTest> filterTests = new List<ColumnFilterTest>() {
                new ColumnFilterTest("OrderId",">11000",77),
                new ColumnFilterTest("ShipperId","1",23, FilterControl.Select),
                new ColumnFilterTest("RequiredDate","14/5/2008",3),
                new ColumnFilterTest("RequiredDate",">14/5/2008",10),
                new ColumnFilterTest("RequiredDate","<14/5/2008",10),
                new ColumnFilterTest("RequiredDate","<=14/5/2008",13),
                new ColumnFilterTest("RequiredDate",">=14/5/2008",13),
                new ColumnFilterTest("OrderId","",13),
                new ColumnFilterTest("RequiredDate","",249),
                new ColumnFilterTest("ShipperId","",830, FilterControl.Select),
            };

            await GridColumnFilter(filterTests, $"postgresql/orders?db={DatabaseName}");
        }


        [Test]
        public async Task SearchDialog()
        {
            List<SearchDialogTest> searchDialogTests = new List<SearchDialogTest>() {
                new SearchDialogTest("OrderId",SearchOperator.GreaterThan,"11000",77),
                new SearchDialogTest("ShipperId",SearchOperator.In,"1",23),
                new SearchDialogTest("RequiredDate",SearchOperator.EqualTo, "2008-05-14",3),
                new SearchDialogTest("RequiredDate",SearchOperator.GreaterThan,"2008-05-14",10),
                new SearchDialogTest("RequiredDate",SearchOperator.LessThan,"2008-05-14",10),
                new SearchDialogTest("RequiredDate",SearchOperator.NotGreaterThan,"2008-05-14",13),
                new SearchDialogTest("RequiredDate",SearchOperator.NotLessThan,"2008-05-14",13),
                new SearchDialogTest("RequiredDate",SearchOperator.Between,"2008-05-01:2008-05-31",20),
                new SearchDialogTest("RequiredDate",SearchOperator.NotBetween,"2008-05-01:2008-05-31",3),
                new SearchDialogTest("RequiredDate",SearchOperator.NotLessThan,"2008-05-14",13),
                new SearchDialogTest("OrderId",null,string.Empty,13),
                new SearchDialogTest("RequiredDate",null,string.Empty,249),
                new SearchDialogTest("ShipperId",SearchOperator.In, string.Empty,830)
            };

            await GridSearchDialogFilter(searchDialogTests, $"postgresql/orders?db={DatabaseName}");
        }
    }
}