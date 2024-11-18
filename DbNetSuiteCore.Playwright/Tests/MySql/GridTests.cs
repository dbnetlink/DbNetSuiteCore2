using DbNetSuiteCore.Playwright.Models;
using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests.MySql
{
    [TestFixture]
    public class GridTests : MySQLDbSetUp
    {
        [Test]
        public async Task QuickSearchTest()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "ger", 41 },
                { "67", 60 },
                { string.Empty, 91}
            };

            await GridQuickSearchTest(searches, $"mysql/customers?db={DatabaseName}");
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
                { "Region", " " },
                { "PostalCode", "10038" },
                { "Country", "Argentina" },
                { "Phone", "(02) 890 12 34" },
                { "Fax", " " }
            };

            await GridHeadingSort(sorts, $"mysql/customers?db={DatabaseName}");
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
                { "Region", new KeyValuePair<string, string>(" ","WY") },
                { "PostalCode", new KeyValuePair<string, string>("10038","10128") },
                { "Country", new KeyValuePair<string, string>("Argentina","Venezuela") },
                { "Phone", new KeyValuePair<string, string>("(02) 890 12 34","981-123456") },
                { "Fax", new KeyValuePair<string, string>(" ","981-789012") }
            };

            await GridHeadingReverseSort(sorts, $"mysql/customers?db={DatabaseName}");
        }

        [Test]
        public async Task ColumnFilter()
        {
            List<ColumnFilterTest> filterTests = new List<ColumnFilterTest>() {
                new ColumnFilterTest("OrderId",">11000",77),
                new ColumnFilterTest("ShipperId","1",23, FilterType.Select),
                new ColumnFilterTest("RequiredDate","14/5/2008",3),
                new ColumnFilterTest("RequiredDate",">14/5/2008",10),
                new ColumnFilterTest("RequiredDate","<14/5/2008",10),
                new ColumnFilterTest("RequiredDate","<=14/5/2008",13),
                new ColumnFilterTest("RequiredDate",">=14/5/2008",13),
                new ColumnFilterTest("OrderId","",13),
                new ColumnFilterTest("RequiredDate","",249),
                new ColumnFilterTest("ShipperId","",830, FilterType.Select),
            };

            await GridColumnFilter(filterTests, $"mysql/orders?db={DatabaseName}");
        }


    }
}