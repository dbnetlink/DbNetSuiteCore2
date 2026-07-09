using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Playwright.Models;
using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests.Excel
{
    public class ExcelGridTests : ComponentTests
    {

        [Test]
        public async Task QuickSearch()
        {
            Dictionary<string, int> searches = new Dictionary<string, int>() {
                { "Henderson", 56 },
                { "SO-20335", 15 },
                { "Highback", 20 },
                { string.Empty, 9994 },
                { "321", 99}
            };

            await GridQuickSearchTest(searches, "excel/Superstore");
        }

        [Test]
        public async Task HeadingSort()
        {
            Dictionary<string, string> sorts = new Dictionary<string, string>()
            {
                { "row_id", "9994"},
                { "order_id","CA-2014-100006" },
                { "order_date", "03/01/2014" },
                { "ship_date", "07/01/2014" },
                { "ship_mode", "First Class" },
                { "customer_id", "AA-10315" },
                { "customer_name", "Aaron Bergman" },
                { "segment", "Consumer" },
                { "city", "Aberdeen" },
                { "state", "Alabama" },
                { "postal_code", "1040" },
                { "region", "Central" },
                { "category", "Furniture" },
                { "sales", "£0.44" },
                { "quantity", "1" },
                { "discount", "0.00%" }
            };

            await GridHeadingSort(sorts, "excel/Superstore");
         }

        [Test]
        public async Task HeadingReverseSort()
        {
            Dictionary<string, KeyValuePair<string, string>> sorts = new Dictionary<string, KeyValuePair<string, string>>()
            {
                { "row_id", new KeyValuePair<string, string>("9994","1")},
                { "order_id",new KeyValuePair<string, string>("CA-2014-100006","US-2017-169551") },
                { "order_date", new KeyValuePair<string, string>("03/01/2014","30/12/2017") },
                { "ship_date", new KeyValuePair<string, string>("07/01/2014","05/01/2018") },
                { "ship_mode", new KeyValuePair<string, string>("First Class","Standard Class") },
                { "customer_id", new KeyValuePair<string, string>("AA-10315","ZD-21925") },
                { "customer_name", new KeyValuePair<string, string>("Aaron Bergman","Zuschuss Donatelli") },
                { "segment", new KeyValuePair<string, string>("Consumer","Home Office") },
                { "city", new KeyValuePair<string, string>("Aberdeen","Yuma") },
                { "state", new KeyValuePair<string, string>("Alabama","Wyoming") },
                { "postal_code", new KeyValuePair<string, string>("1040","99301") },
                { "region", new KeyValuePair<string, string>("Central","West") },
                { "product_id", new KeyValuePair<string, string>("FUR-BO-10000112","TEC-PH-10004977") },
                { "category", new KeyValuePair<string, string>("Furniture","Technology") },
                { "sales", new KeyValuePair<string, string>("£0.44","£22,638.48") },
                { "quantity", new KeyValuePair<string, string>("1","14") },
                { "discount", new KeyValuePair<string, string>("0.00%","80.00%") },
                { "profit", new KeyValuePair<string, string>("-£6,599.98","£8,399.98") },
            };

            await GridHeadingReverseSort(sorts, "excel/Superstore");
        }

        [Test]
        public async Task ColumnFilter()
        {
            List<ColumnFilterTest> filterTests = new List<ColumnFilterTest>() {
                new ColumnFilterTest("order_date","08/11/2016",2),
                new ColumnFilterTest("order_date","",9994),
                new ColumnFilterTest("ship_mode","First Class",1538, FilterControl.Select),
                new ColumnFilterTest("city","Troy",14, FilterControl.Select),
            };

            await GridColumnFilter(filterTests, "excel/Superstore");
        }


        [Test]
        public async Task SearchDialog()
        {
            List<SearchDialogTest> searchDialogTests = new List<SearchDialogTest>() {
                new SearchDialogTest("order_date",SearchOperator.EqualTo, "2016-11-08",2),
                new SearchDialogTest("order_date",null,string.Empty,9994),
                new SearchDialogTest("ship_mode",SearchOperator.EqualTo,"First Class",1538),
                new SearchDialogTest("city",SearchOperator.EqualTo,"Troy",14)
            };

            await GridSearchDialogFilter(searchDialogTests, "excel/Superstore");
        }
    }
}

