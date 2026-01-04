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
         //   await SelectSearchTest(searches, $"json/customers?mode=api");
       //     await SelectSearchTest(searches, $"json/customers?mode=string");
        }

        [Test]
        public async Task GroupTest()
        {
            Dictionary<string, KeyValuePair<int, int>> searches = new Dictionary<string, KeyValuePair<int, int>>() {
                { "aip", new KeyValuePair<int, int>(3,2) },
                { "", new KeyValuePair<int, int>(158,17) }
            };

            await SelectGroupTest(searches, "json/cities");
        }
    }
}