using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests.CSV
{
    public class CsvGridTests : ComponentTests
    {

        [Test]
        public async Task QuickSearchTest()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
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
        public async Task HeadingSort()
        {
            Dictionary<string, string> sorts = new Dictionary<string, string>() {
                { "id", "250"},
                { "name","Afghanistan" },
                { "iso3", "ABW" },
                { "iso2", "AD" },
                { "Numeric_Code", "004" },
                { "phone_code", "+1-242" },
                { "capital", string.Empty },
                { "currency", "AAD" },
                { "currency_name", "Afghan afghani" },
                { "tld", ".ad" },
                { "native", string.Empty },
                { "region", string.Empty },
                { "subregion", string.Empty }
            };

            await GridHeadingSort(sorts, "excel/Countries");
        }

        [Test]
        public async Task HeadingReverseSort()
        {
            Dictionary<string, KeyValuePair<string, string>> sorts = new Dictionary<string, KeyValuePair<string, string>>() {
                { "id", new KeyValuePair<string, string>("250","1") },
                { "name",new KeyValuePair<string, string>("Afghanistan","Zimbabwe") },
                { "iso3", new KeyValuePair<string, string>("ABW","ZWE") },
                { "iso2", new KeyValuePair<string, string>("AD","ZW") },
                { "Numeric_Code", new KeyValuePair<string, string>("004","926") },
                { "phone_code", new KeyValuePair<string, string>("+1-242","998") },
                { "capital", new KeyValuePair<string, string>(string.Empty,"Zagreb") },
                { "currency", new KeyValuePair<string, string>("AAD","ZWL") },
                { "currency_name", new KeyValuePair<string, string>("Afghan afghani","Zimbabwe Dollar") },
                { "tld", new KeyValuePair<string, string>(".ad",".zw") },
                { "native", new KeyValuePair<string, string>(string.Empty,"香港") },
                { "region", new KeyValuePair<string, string>(string.Empty,"Polar") },
                { "subregion", new KeyValuePair<string, string>(string.Empty,"Western Europe") }
            };

            await GridHeadingReverseSort(sorts, "excel/Countries");
        }
    }
}
