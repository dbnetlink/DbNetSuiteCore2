using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Playwright.Models;
using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests.MSSQL
{
    [TestFixture]
    public class GridTests : MSSQLDbSetUp
    {
        [Test]
        public async Task QuickSearch()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "ger", 41 },
                { "67", 17 },
                { string.Empty, 91}
            };

            await GridQuickSearchTest(searches, $"mssql/customers?db={DatabaseName}");
        }

        [Test]
        public async Task HeadingSort()
        {
            Dictionary<string, string> sorts = new Dictionary<string, string>()
            {
                { "CustomerID", "WOLZA"},
                { "CompanyName","Alfreds Futterkiste" },
                { "ContactName", "Alejandra Camino" },
                { "ContactTitle", "Accounting Manager" },
                { "Address", "1 rue Alsace-Lorraine" },
                { "City", "Aachen" },
                { "Region", string.Empty },
                { "PostalCode", string.Empty },
                { "Country", "Argentina" },
                { "Phone", "(02) 201 24 67" },
                { "Fax", string.Empty }
            };

            await GridHeadingSort(sorts, $"mssql/customers?db={DatabaseName}");
        }

        [Test]
        public async Task HeadingReverseSort()
        {
            Dictionary<string, KeyValuePair<string, string>> sorts = new Dictionary<string, KeyValuePair<string, string>>()
            {
                { "CustomerID", new KeyValuePair<string, string>("WOLZA","ALFKI")},
                { "CompanyName", new KeyValuePair<string, string>("Alfreds Futterkiste","Wolski Zajazd") },
                { "ContactName", new KeyValuePair<string, string>("Alejandra Camino","Zbyszek Piestrzeniewicz") },
                { "ContactTitle", new KeyValuePair<string, string>("Accounting Manager","Sales Representative") },
                { "Address", new KeyValuePair<string, string>("1 rue Alsace-Lorraine","Walserweg 21") },
                { "City", new KeyValuePair<string, string>("Aachen","Warszawa") },
                { "Region", new KeyValuePair<string, string>(string.Empty,"WY") },
                { "PostalCode", new KeyValuePair<string, string>(string.Empty,"WX3 6FW") },
                { "Country", new KeyValuePair<string, string>("Argentina","Venezuela") },
                { "Phone", new KeyValuePair<string, string>("(02) 201 24 67","981-443655") },
                { "Fax", new KeyValuePair<string, string>(string.Empty,"981-443655") }
            };

            await GridHeadingReverseSort(sorts, $"mssql/customers?db={DatabaseName}");
        }

        [Test]
        public async Task ColumnFilter()
        {
            List<ColumnFilterTest> filterTests = new List<ColumnFilterTest>() {
                new ColumnFilterTest("CustomerId","BSBEV",10, FilterControl.Select),
                new ColumnFilterTest("OrderDate","16/05/1997",1),
                new ColumnFilterTest("OrderDate","<16/05/1997",4),
                new ColumnFilterTest("OrderDate",">16/05/1997",5),
                new ColumnFilterTest("CustomerId","",538, FilterControl.Select),
                new ColumnFilterTest("ShipRegion","WY",3, FilterControl.Select),
                new ColumnFilterTest("ShipRegion","",538, FilterControl.Select),
                new ColumnFilterTest("OrderDate","  ",830)
            };

            await GridColumnFilter(filterTests, $"mssql/orders?db={DatabaseName}");
        }

        [Test]
        public async Task SearchDialog()
        {
            List<SearchDialogTest> searchDialogTests = new List<SearchDialogTest>() {
                new SearchDialogTest("CustomerId",SearchOperator.In,"BSBEV",10),
                new SearchDialogTest("OrderDate",SearchOperator.EqualTo, "1997-05-16",1),
                new SearchDialogTest("OrderDate",SearchOperator.LessThan,"1997-05-16",4),
                new SearchDialogTest("OrderDate",SearchOperator.GreaterThan,"1997-05-16",5),
                new SearchDialogTest("CustomerId",SearchOperator.In,"",538),
                new SearchDialogTest("ShipRegion",SearchOperator.EqualTo,"WY",3),
                new SearchDialogTest("ShipRegion",null, string.Empty,538),
                new SearchDialogTest("OrderDate",null,"",830),
            };

            await GridSearchDialogFilter(searchDialogTests, $"mssql/orders?db={DatabaseName}");
        }
    }
}