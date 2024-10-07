using NUnit.Framework;

namespace DbNetSuiteCore.Playwright
{
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class CsvGridTests : GridTests
    {

        [Test]
        public async Task QuickSearchTest()
        {
            Dictionary<string,int> searches = new Dictionary<string, int>() { 
                { "loop", 7 }, 
                { string.Empty, 412 }, 
                { "Ordynacka 10", 7 } 
            };

            await GridQuickSearchTest(searches, "csv/invoices");
        }

        [Test]
        public async Task HeadingSort()
        {
            Dictionary<string, string> sorts = new Dictionary<string, string>() { 
                { "customer_id", "1"}, 
                { "first_name","AARON" }, 
                { "last_name", "ABNEY" }, 
                { "email", "AARON.SELBY@sakilacustomer.org" }, 
                { "city", "A Corua (La Corua)" }, 
                { "postal_code", "1027" }, 
                { "create_date", "14/02/06" }, 
                { "last_update", "06/03/21" } 
            };

            await GridHeadingSort(sorts, "csv/index");
        }

        [Test]
        public async Task HeadingReverseSort()
        {
            Dictionary<string, KeyValuePair<string, string>> sorts = new Dictionary<string, KeyValuePair<string, string>>() {
                { "customer_id", new KeyValuePair<string, string>("1","599")  },
                { "first_name", new KeyValuePair<string, string>("AARON","ZACHARY") },
                { "last_name", new KeyValuePair<string, string>("ABNEY","YOUNG") },
                { "email", new KeyValuePair<string, string>("AARON.SELBY@sakilacustomer.org","ZACHARY.HITE@sakilacustomer.org") },
                { "city", new KeyValuePair<string, string>("A Corua (La Corua)","s-Hertogenbosch") },
                { "address", new KeyValuePair<string, string>("1 Valle de Santiago Avenue","999 Sanaa Loop") },
                { "postal_code", new KeyValuePair<string, string>("1027","99865") },
                { "create_date", new KeyValuePair<string, string>("14/02/06","14/02/06") },
                { "last_update", new KeyValuePair<string, string>("06/03/21","08/07/24") }
            };

            await GridHeadingReverseSort(sorts, "csv/index");
          
        }

    }
}
