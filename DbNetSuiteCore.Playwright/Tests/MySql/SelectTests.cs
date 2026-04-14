using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests.MySql
{
    public class SelectTests : MySQLDbSetUp
    {
        [Test]
        public async Task SearchTest()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "customer g", 3 },
                { "xxxxxxx", 0 },
                { "", 91 }
            };

            await SelectSearchTest(searches, $"mysql/customers?db={DatabaseName}");
        }

        [Test]
        public async Task GroupTest()
        {
            Dictionary<string, KeyValuePair<int, int>> searches = new Dictionary<string, KeyValuePair<int, int>>() {
                { "produce", new KeyValuePair<int, int>(5,1) },
                { "product m", new KeyValuePair<int, int>(2,2) },
                { "", new KeyValuePair<int, int>(77,8) }
            };

            await SelectGroupTest(searches, $"mysql/products?db={DatabaseName}");
        }
    }
}
