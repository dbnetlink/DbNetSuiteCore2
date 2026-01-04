using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests.PostgreSql
{
    [TestFixture]
    public class FormTests : PostgreSqlDbSetUp
    {
        public Dictionary<string, string> InsertValues = new Dictionary<string, string>() {
                { "companyname","DbNetLink Limited"},
                { "contactname","Robin Coode"},
                { "contacttitle","Director"},
                { "address","37, Egerton Road"},
                { "city","Bristol"},
                { "region","South West"},
                { "postalcode","BS7 8HN"},
                { "country","UK"},
                { "phone","0117 9624499"},
                { "fax","0117 9624500"},
            };

        [Test]
        public async Task _QuickSearchTest()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "Berlin", 2 },
                { string.Empty, 91 },
                { "1234", 26 },
                { "USA", 13},
                { "xxxx", 0 }
            };

            await FormQuickSearchTest(searches, $"postgresql/customers?db={DatabaseName}");
        }

        [Test]
        public async Task InsertDeleteTest()
        {
            await FormInsertTest(InsertValues, $"postgresql/customers?db={DatabaseName}");
            await FormQuickSearchTest(new Dictionary<string, int>() { { "DbNetLink", 1 } });
            await FormDeleteTest();
            await FormQuickSearchTest(new Dictionary<string, int>() { { "DbNetLink", 0 }, { "", 91 } });
        }
    }
}
