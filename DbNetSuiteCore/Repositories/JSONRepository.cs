using DbNetSuiteCore.Models;
using Newtonsoft.Json;
using System.Data;
using DbNetSuiteCore.Extensions;
using Newtonsoft.Json.Linq;

namespace DbNetSuiteCore.Repositories
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
        public async Task GetRecords(GridModel gridModel, HttpContext httpContext)
        {
            var dataTable = await BuildDataTable(gridModel, httpContext);
            dataTable.FilterAndSort(gridModel);
            gridModel.ConvertEnumLookups();
        }

        public async Task GetRecord(GridModel gridModel, HttpContext httpContext)
        {
            var dataTable = await BuildDataTable(gridModel, httpContext);
            dataTable.FilterWithPrimaryKey(gridModel);
            gridModel.ConvertEnumLookups();
        }

        public async Task<DataTable> GetColumns(GridModel gridModel, HttpContext httpContext)
        {
            return await BuildDataTable(gridModel, httpContext);
        }

        private async Task<DataTable> BuildDataTable(GridModel gridModel, HttpContext httpContext)
        {
            var url = gridModel.Url;

            if (url.StartsWith("/"))
            {
                url = url.Substring(1);
            }

            if (url.StartsWith("http") == false)
            {
                url = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}";
            }

            string token = string.Empty;

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }

            string json = await _httpClient.GetStringAsync(url);

            DataTable? dataTable = new();
            if (string.IsNullOrWhiteSpace(json))
            {
                return dataTable;
            }

            try
            {
                dataTable = Tabulate(json);
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to convert JSON to a table. Please ensure root element is an array", ex);
            }

            if (gridModel.Columns.Any())
            {
                string[] selectedColumns = gridModel.Columns.Select(c => c.Expression).ToArray();

                dataTable = new DataView(dataTable).ToTable(false, selectedColumns);
            }

            return dataTable;
        }

        private DataTable Tabulate(string json)
        {
            JArray srcArray = JArray.Parse(json);
            JArray trgArray = new JArray();
            foreach (JObject row in srcArray.Children<JObject>())
            {
                var cleanRow = new JObject();
                foreach (JProperty column in row.Properties())
                {
                    if (column.Value is JValue)
                    {
                        cleanRow.Add(column.Name, column.Value);
                    }
                }

                trgArray.Add(cleanRow);
            }

            return JsonConvert.DeserializeObject<DataTable>(trgArray.ToString());
        }
    }
}
