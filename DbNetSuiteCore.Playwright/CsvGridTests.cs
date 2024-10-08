using NUnit.Framework;

namespace DbNetSuiteCore.Playwright
{
    public class CsvGridTests : GridTests
    {

        [Test]
        public async Task CsvQuickSearchTest()
        {
            Dictionary<string,int> searches = new Dictionary<string, int>() { 
                { "Polynesia", 10 }, 
                { "EUR", 62 }, 
                { "Česká republika", 1 },
                { string.Empty, 250 },
                { "xxxxxxxx", 0}
            };

            await GridQuickSearchTest(searches, "excel/Countries");

            searches["EUR"] = 74;
            await GridQuickSearchTest(searches, "excel/renderfile?name=countries.csv");
        }

        [Test]
        public async Task CsvHeadingSort()
        {
            Dictionary<string, string> sorts = new Dictionary<string, string>() { 
                { "id", "250"}, 
                { "name","Afghanistan" }, 
                { "iso3", "ABW" }, 
                { "iso2", "AD" }, 
                { "Numeric_Code", "4" }, 
                { "phone_code", string.Empty }, 
                { "capital", string.Empty },
                { "currency", "AAD" },
                { "currency_name", "Afghan afghani" },
                { "currency_symbol", "؋" },
                { "tld", ".ad" },
                { "native", string.Empty },
                { "region", string.Empty },
                { "subregion", string.Empty } 
            };

            await GridHeadingSort(sorts, "excel/Countries");
            await GridHeadingSort(sorts, "excel/renderfile?name=countries.csv");
        }

        [Test]
        public async Task CsvHeadingReverseSort()
        {
            Dictionary<string, KeyValuePair<string, string>> sorts = new Dictionary<string, KeyValuePair<string, string>>() {
                { "id", new KeyValuePair<string, string>("250","1") },
                { "name",new KeyValuePair<string, string>("Afghanistan","Zimbabwe") },
                { "iso3", new KeyValuePair<string, string>("ABW","ZWE") },
                { "iso2", new KeyValuePair<string, string>("AD","ZW") },
                { "Numeric_Code", new KeyValuePair<string, string>("4","926") },
                { "phone_code", new KeyValuePair<string, string>(string.Empty,"1721") },
                { "capital", new KeyValuePair<string, string>(string.Empty,"Zagreb") },
                { "currency", new KeyValuePair<string, string>("AAD","ZWL") },
                { "currency_name", new KeyValuePair<string, string>("Afghan afghani","Zimbabwe Dollar") },
                { "currency_symbol", new KeyValuePair<string, string>("؋","﷼") },
                { "tld", new KeyValuePair<string, string>(".ad",".zw") },
                { "native", new KeyValuePair<string, string>(string.Empty,"臺灣") },
                { "region", new KeyValuePair<string, string>(string.Empty,"Polar") },
                { "subregion", new KeyValuePair<string, string>(string.Empty,"Western Europe") }
            };

            await GridHeadingReverseSort(sorts, "excel/Countries");
            await GridHeadingReverseSort(sorts, "excel/renderfile?name=countries.csv");
        }
    }
}
