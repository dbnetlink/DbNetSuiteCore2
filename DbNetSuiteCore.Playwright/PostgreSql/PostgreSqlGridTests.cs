using NUnit.Framework;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Net;

namespace DbNetSuiteCore.Playwright.PostgreSql
{
    [TestFixture]
    public class PostgreSqlGridTests : PostgreSqlDbSetUp
    {
        [Test]
        public async Task QuickSearchTest()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "ger", 33 },
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

    }
}