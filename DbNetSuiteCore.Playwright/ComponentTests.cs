using Microsoft.Playwright.NUnit;

namespace DbNetSuiteCore.Playwright
{
    public class ComponentTests : PageTest
    {
        protected readonly CustomWebApplicationFactory<Program> _factory;
        protected readonly HttpClient _client;

        private string _baseUrl = "https://localhost:7112/";

        public ComponentTests()
        {
            _factory = new CustomWebApplicationFactory<Program>(_baseUrl);
            _client = _factory.CreateClient();
        }

        public async Task GoToPage(string page)
        {
            await Page.GotoAsync($"{_baseUrl}{page}");
        }
    }
}
