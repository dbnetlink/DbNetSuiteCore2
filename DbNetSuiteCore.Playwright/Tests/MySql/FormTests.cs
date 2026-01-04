using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests.MySql
{
    [TestFixture]
    public class FormTests : MySQLDbSetUp
    {
        public Dictionary<string, string> InsertValues = new Dictionary<string, string>() {
                { "companyName","DbNetLink Limited"},
                { "contactName","Robin Coode"},
                { "contactTitle","Director"},
                { "address","37, Egerton Road"},
                { "city","Bristol"},
                { "region","South West"},
                { "postalCode","BS7 8HN"},
                { "country","UK"},
                { "phone","0117 9624499"},
                { "mobile","07977023328"},
                { "email","info@dbnetlink.co.uk"},
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

            await FormQuickSearchTest(searches, $"mysql/customers?db={DatabaseName}");
        }

        [Test]
        public async Task InsertDeleteTest()
        {
            await FormInsertTest(InsertValues, $"mysql/customers?db={DatabaseName}");
            await FormQuickSearchTest(new Dictionary<string, int>() { { "DbNetLink", 1 } });
            await FormDeleteTest();
            await FormQuickSearchTest(new Dictionary<string, int>() { { "DbNetLink", 0 }, { "", 91 } });
        }
    }
}
