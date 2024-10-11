using NUnit.Framework;

namespace DbNetSuiteCore.Playwright
{
    [TestFixture]
    public class MySqlGridTests : MySQLDbSetUp
    {
        [Test]
        public async Task QuickSearchTest()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "ger", 41 },
                { "67", 60 },
                { string.Empty, 91}
            };

            await GridQuickSearchTest(searches, $"mysql/customers?db={DatabaseName}");
        }
    }
}