using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests.PostgreSql
{
    public class SelectTests : PostgreSqlDbSetUp
    {
        [Test]
        public async Task SearchTest()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "mer x", 6 },
                { "xxxxxxx", 0 },
                { "", 91 }
            };

            await SelectSearchTest(searches, $"postgresql/customers?db={DatabaseName}");
        }

        [Test]
        public async Task GroupTest()
        {
            Dictionary<string, KeyValuePair<int, int>> searches = new Dictionary<string, KeyValuePair<int, int>>() {
                { "seafood", new KeyValuePair<int, int>(12,1) },
                { "product y", new KeyValuePair<int, int>(3,2) },
                { "", new KeyValuePair<int, int>(77,8) }
            };

            await SelectGroupTest(searches, $"postgresql/products?db={DatabaseName}");
        }
    }
}
