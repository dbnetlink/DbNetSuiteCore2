﻿using Microsoft.Playwright;
using NUnit.Framework;

namespace DbNetSuiteCore.Playwright
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

            foreach(string token in searches.Keys)
            {
                await search.FillAsync(token);
                await Page.WaitForResponseAsync(r => r.Url.Contains("gridcontrol.htmx"));

                if (searches[token] == 0)
                {
                    await Expect(Page.Locator("div#no-records")).ToBeVisibleAsync();
                }
                else
                {
                    ILocator rowCount = Page.Locator("input[data-type=\"row-count\"]");
                    await Expect(rowCount).ToHaveValueAsync(searches[token].ToString());
                }
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

        private async Task TestColumnHeadingSort(string columnName, string value)
        {
            ILocator heading = Page.Locator($"th[data-columnname=\"{columnName.ToLower()}\"]");
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