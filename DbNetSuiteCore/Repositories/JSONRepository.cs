using DbNetSuiteCore.Models;
using Newtonsoft.Json;
using System.Data;
using DbNetSuiteCore.Extensions;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Caching.Memory;
using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.Repositories
{
    public class JSONRepository : FileRepository, IJSONRepository
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _memoryCache;

        private static readonly HttpClient _httpClient = new HttpClient();
        public JSONRepository(IConfiguration configuration, IWebHostEnvironment env, IMemoryCache memoryCache)
        {
            _configuration = configuration;
            _env = env;
            _memoryCache = memoryCache;
        }
        public async Task GetRecords(GridModel gridModel, HttpContext httpContext)
        {
            var dataTable = gridModel.Data.Columns.Count > 0 ? gridModel.Data : await BuildDataTable(gridModel, httpContext);
            dataTable.FilterAndSort(gridModel);
            gridModel.ConvertEnumLookups();
            gridModel.GetDistinctLookups();

        }

        public async Task GetRecord(GridModel gridModel, HttpContext httpContext)
        {
            var dataTable = await BuildDataTable(gridModel, httpContext);
            dataTable.FilterWithPrimaryKey(gridModel);
            gridModel.ConvertEnumLookups();
        }

        public async Task<DataTable> GetColumns(GridModel gridModel, HttpContext httpContext)
        {
            gridModel.Data = await BuildDataTable(gridModel, httpContext);
            return gridModel.Data;
        }

        private async Task<DataTable> BuildDataTable(GridModel gridModel, HttpContext httpContext)
        {
            if (gridModel.Cache && _memoryCache.TryGetValue(gridModel.Id, out DataTable dataTable))
            {
                return dataTable;
            }
            else
            {
                dataTable = await JsonToDataTable(gridModel, httpContext);

                if (gridModel.Cache)
                {
                    _memoryCache.Set(gridModel.Id, dataTable, GetCacheOptions(gridModel));
                }

                return dataTable;
            }
        }

        private async Task<DataTable> JsonToDataTable(GridModel gridModel, HttpContext httpContext)
        {
            string json = string.Empty;

            if (string.IsNullOrEmpty(gridModel.Url))
            {
                json = gridModel.JSON;
            }
            else
            {
                if (TextHelper.IsAbsolutePath(gridModel.Url))
                {
                    json = File.ReadAllText(gridModel.Url);
                }
                else
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

                    json = await _httpClient.GetStringAsync(url);
                }
            }

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
