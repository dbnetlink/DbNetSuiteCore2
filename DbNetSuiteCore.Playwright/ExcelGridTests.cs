using NUnit.Framework;

namespace DbNetSuiteCore.Playwright
{
    public class ExcelGridTests : GridTests
    {

        [Test]
        public async Task ExcelQuickSearchTest()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "Henderson", 56 },
                { "SO-20335", 15 },
                { "Highback", 20 },
                { string.Empty, 9994 },
                { "321", 81}
            };

            await GridQuickSearchTest(searches, "excel/Superstore?ext=xlsx");
            await GridQuickSearchTest(searches, "excel/Superstore?ext=xls");
            await GridQuickSearchTest(searches, "excel/Superstore?ext=csv");
        }

        [Test]
        public async Task ExcelHeadingSort()
        {
            Dictionary<string, string> sorts = new Dictionary<string, string>() 
            {
                { "row id", "9994"},
                { "order id","CA-2014-100006" },
                { "order date", "03/01/2014" },
                { "ship date", "07/01/2014" },
                { "ship mode", "First Class" },
                { "customer id", "AA-10315" },
                { "customer name", "Aaron Bergman" },
                { "segment", "Consumer" },
                { "city", "Aberdeen" },
                { "state", "Alabama" },
                { "postal code", string.Empty },
                { "region", "Central" },
                { "category", "Furniture" },
                { "sales", "£0.44" },
                { "quantity", "1" },
                { "discount", "0.00%" }
            };

            await GridHeadingSort(sorts, "excel/Superstore?ext=xlsx");
            await GridHeadingSort(sorts, "excel/Superstore?ext=xls");

            sorts["postal code"] = "1040";
            await GridHeadingSort(sorts, "excel/Superstore?ext=csv");
        }

        [Test]
        public async Task ExcelHeadingReverseSort()
        {
            Dictionary<string, KeyValuePair<string, string>> sorts = new Dictionary<string, KeyValuePair<string, string>>() 
            {
                { "row id", new KeyValuePair<string, string>("9994","1")},
                { "order id",new KeyValuePair<string, string>("CA-2014-100006","US-2017-169551") },
                { "order date", new KeyValuePair<string, string>("03/01/2014","30/12/2017") },
                { "ship date", new KeyValuePair<string, string>("07/01/2014","05/01/2018") },
                { "ship mode", new KeyValuePair<string, string>("First Class","Standard Class") },
                { "customer id", new KeyValuePair<string, string>("AA-10315","ZD-21925") },
                { "customer name", new KeyValuePair<string, string>("Aaron Bergman","Zuschuss Donatelli") },
                { "segment", new KeyValuePair<string, string>("Consumer","Home Office") },
                { "city", new KeyValuePair<string, string>("Aberdeen","Yuma") },
                { "state", new KeyValuePair<string, string>("Alabama","Wyoming") },
                { "postal code", new KeyValuePair<string, string>(string.Empty,"99301") },
                { "region", new KeyValuePair<string, string>("Central","West") },
                { "product id", new KeyValuePair<string, string>("FUR-BO-10000112","TEC-PH-10004977") },
                { "category", new KeyValuePair<string, string>("Furniture","Technology") },
                { "sales", new KeyValuePair<string, string>("£0.44","£22,638.48") },
                { "quantity", new KeyValuePair<string, string>("1","14") },
                { "discount", new KeyValuePair<string, string>("0.00%","80.00%") },
                { "profit", new KeyValuePair<string, string>("-£6,599.98","£8,399.98") },
            };

            await GridHeadingReverseSort(sorts, "excel/Superstore?ext=xlsx");
            await GridHeadingReverseSort(sorts, "excel/Superstore?ext=xls");

            sorts["postal code"] = new KeyValuePair<string, string>("1040", "99301");
            await GridHeadingReverseSort(sorts, "excel/Superstore?ext=csv");
        }
    }
}

