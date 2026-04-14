using DbNetSuiteCore.Playwright.Tests.PostgreSql;
using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests.SQLite
{
    [TestFixture]
    public class FormTests : SQLiteDbSetUp
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
                { "Phone","123-456-7890"},
                { "Fax","0117 9624500"}
            };

        [Test]
        public async Task QuickSearchTest()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "Berlin", 2 },
                { string.Empty, 91 },
                { "743", 2 },
                { "USA", 13},
                { "xxxx", 0 }
            };

            await FormQuickSearchTest(searches, $"sqlite/customers");
       //     await FormQuickSearchTest(searches, $"customers", true);
        }

        [Test]
        public async Task InsertDeleteTest()
        {
            await FormInsertTest(InsertValues, $"sqlite/customers?db={DatabaseName}");
            await FormQuickSearchTest(new Dictionary<string, int>() { { "DBNET", 1 }});
            await FormDeleteTest();
            await FormQuickSearchTest(new Dictionary<string, int>() {{ "DBNET", 0 }, { "", 91 }});

            await FormInsertTest(InsertValues, $"customers?db={DatabaseName}", true);
            await FormQuickSearchTest(new Dictionary<string, int>() { { "DBNET", 1 } });
            await FormDeleteTest();
            await FormQuickSearchTest(new Dictionary<string, int>() { { "DBNET", 0 }, { "", 91 } });
        }
    }
}
