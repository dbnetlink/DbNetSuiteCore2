using NUnit.Framework;

namespace DbNetSuiteCore.Playwright
{
    [TestFixture]
    public class MSSQLGridTests : MSSQLDbSetUp
    {
        [Test]
        public async Task QuickSearchTest()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "ger", 41 },
                { "67", 17 },
                { string.Empty, 91}
            };

            await GridQuickSearchTest(searches, $"mssql/customers?db={DatabaseName}");
        }
    }
}