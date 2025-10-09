using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Playwright.Enums;
using DbNetSuiteCore.Playwright.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace DbNetSuiteCore.Playwright.Tests
{
    public class ComponentTests : PageTest
    {
        private int _port;
        protected CustomWebApplicationFactory<Program> _factory;
        protected HttpClient _client;
        public int Port => _port;
        private string _baseUrl = string.Empty;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _port = FreeTcpPort();
            _baseUrl = $"https://localhost:{_port}/";
            _factory = new CustomWebApplicationFactory<Program>(_baseUrl);
            _client = _factory.CreateClient();
        }
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _factory.Dispose();
        }

        public async Task GoToPage(string page, ComponentType componentType = ComponentType.Grid, bool mvc = false)
        {
            Playwright.Selectors.SetTestIdAttribute("button-type");
            string url = $"{_baseUrl}{componentType.ToString().ToLower()}control/{page}";

            if (mvc)
            {
                url = $"{_baseUrl}{componentType.ToString().ToLower()}/{page}";
            }
            Page.SetDefaultTimeout(10000);
            await Page.GotoAsync(url);
        }

        public async Task GoToPage(string page, bool mvc = false)
        {
            await GoToPage(page, ComponentType.Grid, mvc);
        }

        private int FreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        protected string SolutionFolder()
        {
            return Folder(4);
        }
        protected string ProjectFolder()
        {
            return Folder(3);
        }

        private string Folder(int truncate)
        {
            var folders = TestContext.CurrentContext.TestDirectory.Split("\\").ToList();
            folders.RemoveRange(folders.Count - truncate, truncate);
            return $"{string.Join("\\", folders)}";
        }

        protected async Task GridQuickSearchTest(Dictionary<string, int> searches, string page, bool mvc = false)
        {
            await GoToPage(page, mvc);
            ILocator search = Page.GetByPlaceholder("Search");

            foreach (string token in searches.Keys)
            {
                await search.FillAsync(token);
                await TestRowCount(searches[token]);
            }
        }

        protected async Task GridHeadingSort(Dictionary<string, string> sorts, string page, bool mvc = false)
        {
            await GoToPage(page, mvc);

            foreach (string columnName in sorts.Keys)
            {
                await TestColumnHeadingSort(columnName, sorts[columnName]);
            }
        }

        protected async Task GridHeadingReverseSort(Dictionary<string, KeyValuePair<string, string>> sorts, string page, bool mvc = false)
        {
            await GoToPage(page, mvc);

            foreach (string columnName in sorts.Keys)
            {
                foreach (string value in new List<string>() { sorts[columnName].Key, sorts[columnName].Value })
                {
                    await TestColumnHeadingSort(columnName, value);
                }
            }
        }

        protected async Task GridColumnFilter(List<ColumnFilterTest> columnFilterTests, string page, bool mvc = false)
        {
            await GoToPage(page, mvc);

            foreach (ColumnFilterTest columnFilterTest in columnFilterTests)
            {
                await TestColumnFilter(columnFilterTest);
            }
        }

        protected async Task GridSearchDialogFilter(List<SearchDialogTest> searchDialogTests, string page, bool mvc = false)
        {
            await GoToPage(page, mvc);

            foreach (SearchDialogTest searchDialogTest in searchDialogTests)
            {
                await TestSearchDialog(searchDialogTest);
            }
        }

        protected async Task SelectSearchTest(Dictionary<string, int> searches, string page, bool mvc = false)
        {
            await GoToPage(page, ComponentType.Select, mvc);
            ILocator search = Page.GetByPlaceholder("Search");

            foreach (string token in searches.Keys)
            {
                await search.FillAsync(token);
                await TestTagCount(new Dictionary<string, int> { { "option", searches[token] } });
            }
        }

        protected async Task SelectGroupTest(Dictionary<string, KeyValuePair<int, int>> searches, string page, bool mvc = false)
        {
            await GoToPage(page, ComponentType.Select, mvc);
            ILocator search = Page.GetByPlaceholder("Search");

            foreach (string token in searches.Keys)
            {
                await search.FillAsync(token); 
                await TestTagCount(new Dictionary<string, int> { { "option", searches[token].Key }, { "optgroup", searches[token].Value } });
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

        //    var element = await filter.ElementHandleAsync();
            if (columnFilterTest.FilterType == FilterControl.Select)
            {
                await filter.SelectOptionAsync(columnFilterTest.FilterValue);
            }
            else
            {
                await filter.FillAsync(columnFilterTest.FilterValue);
            }

            if (columnFilterTest.ErrorString != null)
            {
                await TestFilterErrorString(filter, columnFilterTest.ErrorString.Value);
            }
            else
            {
                await TestRowCount(columnFilterTest.ExpectedRowCount);
            }
        }

        private async Task TestSearchDialog(SearchDialogTest searchDialogTest)
        {
            await Page.GetByTestId("search").ClickAsync();
            ILocator row = GetSearchDialogRow(searchDialogTest.ColumnName);
            ILocator select = row.Locator("select");
            ILocator input1 = row.Locator("input[name='searchDialogValue1']");
            ILocator input2 = row.Locator("input[name='searchDialogValue2']");
            ILocator lookupBtn = row.Locator("button");

            await select.SelectOptionAsync(searchDialogTest.SearchOperator?.ToString() ?? string.Empty);

            switch(searchDialogTest.SearchOperator)
            {
                case SearchOperator.True:
                case SearchOperator.False:
                    break;
                case SearchOperator.In:
                case SearchOperator.NotIn:
                    await lookupBtn.ClickAsync();
                    ILocator lookupSelect = Page.Locator($"dialog.lookup-dialog select");

                    await lookupSelect.SelectOptionAsync(Array.Empty<string>());

                    if (string.IsNullOrEmpty(searchDialogTest.FilterValue) == false)
                    {
                        foreach (string value in searchDialogTest.FilterValue.Split(","))
                        {
                            await lookupSelect.SelectOptionAsync(value);
                        }
                    }
                    ILocator selectBtn = Page.Locator($"dialog.lookup-dialog button[button-type=\"select\"]");
                    await selectBtn.ClickAsync();
                    break;
                case SearchOperator.Between:
                case SearchOperator.NotBetween:
                    await input1.FillAsync(searchDialogTest.FilterValue.Split(":").First());
                    await input2.FillAsync(searchDialogTest.FilterValue.Split(":").Last());
                    break;
                default:
                    await input1.FillAsync(searchDialogTest.FilterValue);
                    break;
            }

            await Page.GetByTestId("search-dialog-apply").ClickAsync();
            await TestRowCount(searchDialogTest.ExpectedRowCount);

        }

        private async Task TestFilterErrorString(ILocator filter, ResourceNames errorString)
        {
            var columnKey = await filter.GetAttributeAsync("data-key");
            var inputSelector = $"tr.filter-row input[data-key=\"{columnKey}\"]";

            // Wait for the title and style attributes to be set
            await Page.EvaluateAsync(@"selector => {
            return new Promise(resolve => {
                const checkAttributes = () => {
                    const element = document.querySelector(selector);
                    if (element.hasAttribute('title') && element.hasAttribute('style') && element.getAttribute('title') !== '' && element.getAttribute('style') !== '') {
                        resolve();
                    } else {
                        requestAnimationFrame(checkAttributes);
                    }
                };
                checkAttributes();
            });
        }", inputSelector);

            var title = await filter.GetAttributeAsync("title") ?? string.Empty;
            var style = await filter.GetAttributeAsync("style") ?? string.Empty;
            Assert.That(style, Is.EqualTo("background-color: rgb(252, 165, 165);"));
            Assert.That(title, Is.EqualTo(ResourceHelper.GetResourceString(errorString)));
        }

        private ILocator GetHeading(string columnName)
        {
            return Page.Locator($"th[data-columnname=\"{columnName.ToLower()}\"]");
        }

        private ILocator GetSearchDialogRow(string columnName)
        {
            return Page.Locator($"dialog.search-dialog tr[data-columnname=\"{columnName.ToLower()}\"]");
        }

        private async Task TestRowCount(int expectedRowCount, string type = "row")
        {
            await Page.WaitForResponseAsync(r => r.Url.Contains("control.htmx"));
            if (expectedRowCount == 0)
            {
                await Expect(Page.Locator("div#no-records")).ToBeVisibleAsync();
            }
            else
            {
                ILocator rowCount = Page.Locator($"input[data-type=\"{type}-count\"]");
                await Expect(rowCount).ToHaveValueAsync(expectedRowCount.ToString());
            }
        }

        private async Task TestTagCount(Dictionary<string,int> tagCounts)
        {
            await Page.WaitForResponseAsync(r => IsSelectUrl("selectcontrol.htmx"));

            foreach (var tag in tagCounts.Keys)
            {
                if (tagCounts[tag] == 0)
                {
                    await Expect(Page.Locator(tag).Nth(0)).ToHaveTextAsync("No records found");
                }
                else
                {
                    var optionCount = await Page.Locator(tag).CountAsync();
                    Assert.That(optionCount, Is.EqualTo(tagCounts[tag]));
                }
            }
        }

        private bool IsSelectUrl(string url)
        {
            return url.Contains("selectcontrol.htmx");
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

        protected async Task FormQuickSearchTest(Dictionary<string, int> searches, string page = "", bool mvc = false)
        {
            if (string.IsNullOrEmpty(page) == false)
            {
                await GoToPage(page, ComponentType.Form, mvc);
            }

            ILocator search = Page.GetByPlaceholder("Search");

            foreach (string token in searches.Keys)
            {
                await search.FillAsync(token);
                await TestRowCount(searches[token],"record");
            }
        }

        protected async Task FormInsertTest(Dictionary<string, string> values, string page, bool mvc = false)
        {
            await GoToPage(page, ComponentType.Form, mvc);
            await Page.GetByTestId("insert").ClickAsync();
            await Page.WaitForResponseAsync(r => r.Url.Contains("control.htmx"));

            foreach (string columnName in values.Keys)
            {
                ILocator input = Page.Locator($"[name=\"_{columnName.ToLower()}\"]");
                await input.FillAsync(values[columnName]);
            }

            await Page.GetByTestId("apply").ClickAsync();
            await TestRowCount(92, "record");
        }

        protected async Task FormDeleteTest(string page = "", bool mvc = false)
        {
            if (string.IsNullOrEmpty(page) == false)
            {
                await GoToPage(page, ComponentType.Form, mvc);
            }
            await Page.GetByTestId("delete").ClickAsync();
            await Page.GetByTestId("confirm").ClickAsync();
        }
    }
}
