using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests.MongoDB
{
    public class SelectTests : MongoDBDbSetUp
    {
        [Test]
        public async Task SearchTest()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "market", 4 },
                { "xxxxxxx", 0 },
                { "", 91 }
            };

            await SelectSearchTest(searches, $"mongodb/customers?db={DatabaseName}");
        }

        [Test]
        public async Task GroupTest()
        {
            Dictionary<string, KeyValuePair<int, int>> searches = new Dictionary<string, KeyValuePair<int, int>>() {
                { "sil", new KeyValuePair<int, int>(3,1) },
                { "", new KeyValuePair<int, int>(77,8) }
            };

            await SelectGroupTest(searches, $"mongodb/products?db={DatabaseName}");
        }
    }
}
