using NUnit.Framework;

namespace DbNetSuiteCore.Playwright
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
    }
}