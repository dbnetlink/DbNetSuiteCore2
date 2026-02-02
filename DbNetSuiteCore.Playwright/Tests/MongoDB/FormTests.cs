using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests.MongoDB
{
    [TestFixture]
    public class FormTests : MongoDBDbSetUp
    {
        public Dictionary<string, string> InsertValues = new Dictionary<string, string>() {
            { "CustomerID","DBNET"},
            { "CompanyName","DbNetLink Limited"},
            { "ContactName","Robin Coode"},
            { "ContactTitle","Director"},
            { "Address","37, Egerton Road"},
            { "City","Bristol"},
            { "Region","South West"},
            { "PostalCode","BS7 8HN"},
            { "Country","UK"},
            { "Phone","0117 9624499"},
            { "Fax","0117 9624500"},
        };

        [Test]
        public async Task QuickSearchTest()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "Berlin", 2 },
                { string.Empty, 91 },
                { "432", 3 },
                { "USA", 13},
                { "xxxx", 0 }
            };

            await FormQuickSearchTest(searches, $"mongodb/customers?db={DatabaseName}");
        }

        [Test]
        public async Task InsertDeleteTest()
        {
            await FormInsertTest(InsertValues, $"mongodb/customers?db={DatabaseName}");
            await FormQuickSearchTest(new Dictionary<string, int>() { { "DbNetLink", 1 } });
            await FormDeleteTest();
            await FormQuickSearchTest(new Dictionary<string, int>() { { "DbNetLink", 0 }, { "", 91 } });
        }
    }
}
