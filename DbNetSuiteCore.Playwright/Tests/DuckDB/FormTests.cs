using DbNetSuiteCore.Playwright.Tests.PostgreSql;
using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests.DuckDB
{
    [TestFixture]
    public class FormTests : DuckDbSetUp
    {
        public Dictionary<string, string> InsertValues = new Dictionary<string, string>() {
                { "customer_id","1000"},
                { "first_name","DbNetLink Limited"},
                { "last_name","Robin Coode"},
                { "company","Director"},
                { "address","37, Egerton Road"},
                { "city","Bristol"},
                { "country","United Kingdom"},
                { "postal_code","BS6 7QE"},
                { "phone","0117 1111111"},
                { "fax","0117 9624500"},
                { "email","robin.coode@dnv.com"},
                { "support_rep_id","4"}
            };
        [Test]
        public async Task QuickSearchTest()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "Berlin", 2 },
                { string.Empty, 60 },
                { "5555", 2 },
                { "USA", 13},
                { "xxxx", 0 }
            };

            await FormQuickSearchTest(searches, $"duckdb/customers");
        }

        [Test]
        public async Task InsertDeleteTest()
        {
            await FormInsertTest(InsertValues, $"duckdb/customers", false, 61);
            await FormQuickSearchTest(new Dictionary<string, int>() { { "DBNET", 1 }});
            await FormDeleteTest();
            await FormQuickSearchTest(new Dictionary<string, int>() {{ "DBNET", 0 }, { "", 60 }});
        }
    }
}
