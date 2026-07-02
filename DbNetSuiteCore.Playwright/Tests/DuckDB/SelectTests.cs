using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests.DuckDB
{
    public class SelectTests : DuckDbSetUp
    {
        [Test]
        public async Task SearchTest()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "berlin", 3 },
                { "xxxxxxx", 0 },
                { "", 61 }
            };

            await SelectSearchTest(searches, $"duckdb/customers");
        }

        [Test]
        public async Task GroupTest()
        {
            Dictionary<string, KeyValuePair<int, int>> searches = new Dictionary<string, KeyValuePair<int, int>>() {
                { "lover", new KeyValuePair<int, int>(23,6) },
                { "please", new KeyValuePair<int, int>(6,6) }
            };

            await SelectGroupTest(searches, $"duckdb/products");
        }
    }
}
