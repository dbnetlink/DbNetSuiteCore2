using Microsoft.Playwright.NUnit;
using System.Net.Sockets;
using System.Net;
using NUnit.Framework;

namespace DbNetSuiteCore.Playwright
{
    public class ComponentTests : PageTest
    {
        protected CustomWebApplicationFactory<Program> _factory;
        protected HttpClient _client;

        private string _baseUrl = $"https://localhost:{FreeTcpPort()}/";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _factory = new CustomWebApplicationFactory<Program>(_baseUrl);
            _client = _factory.CreateClient();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _factory.Dispose();
        }


        public async Task GoToPage(string page)
        {
            await Page.GotoAsync($"{_baseUrl}{page}");
        }

        static int FreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }
}
