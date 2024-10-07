using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace DbNetSuiteCore.Playwright
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        private readonly string _hostUrl;

        public CustomWebApplicationFactory(string hostUrl)
        {
            _hostUrl = hostUrl;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseUrls(_hostUrl);
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var dummyHost = builder.Build();

            builder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseKestrel());

            var host = builder.Build();
            host.Start();

            return dummyHost;
        }
    }

}
