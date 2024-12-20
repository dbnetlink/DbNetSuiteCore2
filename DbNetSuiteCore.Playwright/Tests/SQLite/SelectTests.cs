using DbNetSuiteCore.Playwright.Tests.PostgreSql;
using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests.SQLite
{
    public class SelectTests : SQLiteDbSetUp
    {
        [Test]
        public async Task SearchTest()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "berlin", 3 },
                { "xxxxxxx", 0 },
                { "", 92 }
            };

            await SelectSearchTest(searches, $"sqlite/customers?db={DatabaseName}");
        }

        [Test]
        public async Task GroupTest()
        {
            Dictionary<string, KeyValuePair<int, int>> searches = new Dictionary<string, KeyValuePair<int, int>>() {
                { "condiments", new KeyValuePair<int, int>(12,1) },
                { "", new KeyValuePair<int, int>(77,8) }
            };

            await SelectGroupTest(searches, $"sqlite/products?db={DatabaseName}");
        }
    }
}
