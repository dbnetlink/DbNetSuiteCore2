using DbNetTimeCore.Models;
using Newtonsoft.Json;
using System.Data;

namespace DbNetTimeCore.Repositories
{
    public class JSONRepository : IJSONRepository
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private static readonly HttpClient _httpClient = new HttpClient();
        public JSONRepository(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }
        public async Task<DataTable> GetRecords(GridModel gridModel, HttpContext httpContext)
        {
            return await DownloadJson(gridModel.Url, httpContext);
        }

        public async Task<DataTable> GetColumns(GridModel gridModel, HttpContext httpContext)
        {
            return await DownloadJson(gridModel.Url, httpContext);
        }

        private async Task<DataTable> DownloadJson(string url, HttpContext httpContext)
        {
            if (url.StartsWith("http") == false)
            {
                url = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}";
            }
            string json = await _httpClient.GetStringAsync(url);

            DataTable? dataTable = new();
            if (string.IsNullOrWhiteSpace(json))
            {
                return dataTable;
            }
            dataTable = JsonConvert.DeserializeObject<DataTable>(json);
            return dataTable;
        }
    }
}
