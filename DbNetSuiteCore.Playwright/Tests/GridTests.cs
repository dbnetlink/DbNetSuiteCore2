using DbNetSuiteCore.Playwright.Models;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Office2010.Excel;
using Irony.Parsing;
using Microsoft.Playwright;
using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests
{
    public class GridTests : ComponentTests
    {
        public GridTests() : base()
        {
        }

        protected async Task GridQuickSearchTest(Dictionary<string, int> searches, string page)
        {
            await GoToPage(page);
            ILocator search = Page.GetByPlaceholder("Search");

            foreach (string token in searches.Keys)
            {
                await search.FillAsync(token);
                await TestRowCount(searches[token]);
            }
        }

        protected async Task GridHeadingSort(Dictionary<string, string> sorts, string page)
        {
            await GoToPage(page);

            foreach (string columnName in sorts.Keys)
            {
                await TestColumnHeadingSort(columnName, sorts[columnName]);
            }
        }

        protected async Task GridHeadingReverseSort(Dictionary<string, KeyValuePair<string, string>> sorts, string page)
        {
            await GoToPage(page);

            foreach (string columnName in sorts.Keys)
            {
                foreach (string value in new List<string>() { sorts[columnName].Key, sorts[columnName].Value })
                {
                    await TestColumnHeadingSort(columnName, value);
                }
            }
        }

        protected async Task GridColumnFilter(List<ColumnFilterTest> columnFilterTests, string page)
        {
            await GoToPage(page);

            foreach (ColumnFilterTest columnFilterTest in columnFilterTests)
            {
                await TestColumnFilter(columnFilterTest);
            }
        }

        private async Task TestColumnHeadingSort(string columnName, string value)
        {
            ILocator heading = GetHeading(columnName);
            var cellIndex = await GetCellIndex(heading);

            if (cellIndex == -1)
            {
                throw new Exception($"Heading => {columnName} not found");
            }
            await heading.ClickAsync();
            await Page.WaitForResponseAsync(r => r.Url.Contains("gridcontrol.htmx"));

            var firstColumnCell = Page.Locator($"tr.grid-row").Nth(0).Locator("td").Nth(cellIndex);
            await Expect(firstColumnCell).ToHaveTextAsync(value);
        }

        private async Task TestColumnFilter(ColumnFilterTest columnFilterTest)
        {
            ILocator heading = GetHeading(columnFilterTest.ColumnName);
            var columnKey = await heading.GetAttributeAsync("data-key");
            ILocator filter = Page.Locator($"tr.filter-row {columnFilterTest.FilterType.ToString().ToLower()}[data-key=\"{columnKey}\"]");

            var element =  await filter.ElementHandleAsync();
            if (columnFilterTest.FilterType == FilterType.Select)
            {
                await filter.SelectOptionAsync(columnFilterTest.FilterValue);
            }
            else
            {
                await filter.FillAsync(columnFilterTest.FilterValue);
            }
            await TestRowCount(columnFilterTest.ExpectedRowCount);
        }

        private ILocator GetHeading(string columnName)
        {
            return Page.Locator($"th[data-columnname=\"{columnName.ToLower()}\"]");
        }

        private async Task TestRowCount(int expectedRowCount)
        {
            await Page.WaitForResponseAsync(r => r.Url.Contains("gridcontrol.htmx"));

            if (expectedRowCount == 0)
            {
                await Expect(Page.Locator("div#no-records")).ToBeVisibleAsync();
            }
            else
            {
                ILocator rowCount = Page.Locator("input[data-type=\"row-count\"]");
                await Expect(rowCount).ToHaveValueAsync(expectedRowCount.ToString());
            }
        }

        private async Task<int> GetCellIndex(ILocator heading)
        {
            string columnName = await heading.GetAttributeAsync("data-columnname");

            ILocator headings = Page.Locator($"tr.heading-row th");

            for (int i = 0; i < await headings.CountAsync(); i++)
            {
                if (await headings.Nth(i).GetAttributeAsync("data-columnname") == columnName)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}