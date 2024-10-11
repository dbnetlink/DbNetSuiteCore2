using Microsoft.Playwright.NUnit;
using System.Net.Sockets;
using System.Net;
using NUnit.Framework;

namespace DbNetSuiteCore.Playwright
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

        public async Task GoToPage(string page)
        {
            await Page.GotoAsync($"{_baseUrl}{page}");
        }

        private int FreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }
}
