using DbNetSuiteCore.Playwright.Models;
using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests.MongoDB
{
    [TestFixture]
    public class MongoDBGridTests : MongoDBDbSetUp
    {
        [Test]
        public async Task QuickSearchTest()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "gin", 3 },
                { "56", 60 },
                { "6-7", 7 },
                { "SP", 13 },
                { string.Empty, 91}
            };

            await GridQuickSearchTest(searches, $"mongodb/customers?db={DatabaseName}");
        }

        [Test]
        public async Task HeadingSort()
        {
            Dictionary<string, string> sorts = new Dictionary<string, string>()
            {
                { "entityid", "91"},
                { "companyname","Customer AHPOP" },
                { "contactname", "Allen, Michael" },
                { "contacttitle", "Accounting Manager" },
                { "phone", "(02) 890 12 34" },
                { "fax", "" },
                { "address", "0123 Grizzly Peak Rd." },
                { "postalcode", "10038" },
                { "city", "Aachen" },
                { "region", "" },
                { "country", "Argentina" }
            };

            await GridHeadingSort(sorts, $"mongodb/customers?db={DatabaseName}");
        }

        [Test]
        public async Task HeadingReverseSort()
        {
            Dictionary<string, KeyValuePair<string, string>> sorts = new Dictionary<string, KeyValuePair<string, string>>()
            {
                { "entityid", new KeyValuePair<string, string>("91","1") },
                { "companyname",new KeyValuePair<string, string>("Customer AHPOP","Customer ZRNDE") },
                { "contactname", new KeyValuePair<string, string>("Allen, Michael","Young, Robin") },
                { "contacttitle", new KeyValuePair<string, string>("Accounting Manager","Sales Representative") },
                { "phone", new KeyValuePair<string, string>("(02) 890 12 34","981-123456") },
                { "fax", new KeyValuePair<string, string>("","981-789012") },
                { "address", new KeyValuePair<string, string>("0123 Grizzly Peak Rd.","Åkergatan 5678") },
                { "postalcode", new KeyValuePair<string, string>("10038","10128") },
                { "city", new KeyValuePair<string, string>("Aachen","Århus") },
                { "region", new KeyValuePair<string, string>("","WY") },
                { "country", new KeyValuePair<string, string>("Argentina","Venezuela") }
            };

            await GridHeadingReverseSort(sorts, $"mongodb/customers?db={DatabaseName}");
        }

        [Test]
        public async Task ColumnFilter()
        {
            List<ColumnFilterTest> filterTests = new List<ColumnFilterTest>() {
                new ColumnFilterTest("entityid",">35",56),
                new ColumnFilterTest("entityid","<31",0),
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

            await GridColumnFilter(filterTests, $"mongodb/orders?db={DatabaseName}");
        }

    }
}