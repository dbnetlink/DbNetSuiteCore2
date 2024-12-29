using DbNetSuiteCore.Playwright.Models;
using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests.JSON
{
    public class SelectTests : ComponentTests
    {
        [Test]
        public async Task SearchTest()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "com", 5 },
                { "xxxxxxx", 0 },
                { "", 91 }
            };

            await SelectSearchTest(searches, "json/customers");
            await SelectSearchTest(searches, $"json/customers?port={Port}");
            await SelectSearchTest(searches, $"json/customers?port={Port}&mode=string");
        }

        [Test]
        public async Task GroupTest()
        {
            Dictionary<string, KeyValuePair<int, int>> searches = new Dictionary<string, KeyValuePair<int, int>>() {
                { "wood", new KeyValuePair<int, int>(14,10) },
                { "", new KeyValuePair<int, int>(604,49) }
            };

            await SelectGroupTest(searches, "json/cities");
            await SelectGroupTest(searches, $"json/cities?port={Port}");
            await SelectGroupTest(searches, $"json/cities?port={Port}&mode=string");
        }
    }
}