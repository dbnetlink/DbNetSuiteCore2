using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Text.Json;

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
        public async Task GetRecords(ComponentModel componentModel, HttpContext httpContext)
        {
            var dataTable = componentModel.Data.Columns.Count > 0 ? componentModel.Data : await BuildDataTable(componentModel, httpContext);
            if (componentModel is GridModel)
            {
                var gridModel = (GridModel)componentModel;
                dataTable.FilterAndSort(gridModel);
                gridModel.ConvertEnumLookups();
                gridModel.GetDistinctLookups();
            }

            if (componentModel is SelectModel)
            {
                var selectModel = (SelectModel)componentModel;
                if (selectModel.Distinct)
                {
                    var columnNames = dataTable.Columns.Cast<DataColumn>().Select(dc => dc.ColumnName).ToArray();
                    dataTable = dataTable.DefaultView.ToTable(true, columnNames);
                }
                dataTable.FilterAndSort(selectModel);
                selectModel.ConvertEnumLookups();
            }
        }
        public async Task GetRecord(ComponentModel componentModel, HttpContext httpContext)
        {
            var dataTable = await BuildDataTable(componentModel, httpContext);
            dataTable.FilterWithPrimaryKey(componentModel);
            componentModel.ConvertEnumLookups();
        }

        public async Task<DataTable> GetColumns(ComponentModel componentModel, HttpContext httpContext)
        {
            componentModel.Data = await BuildDataTable(componentModel, httpContext);
            return componentModel.Data;
        }

        private async Task<DataTable> BuildDataTable(ComponentModel componentModel, HttpContext httpContext)
        {
            if (componentModel.Cache && _memoryCache.TryGetValue(componentModel.Id, out DataTable dataTable))
            {
                return dataTable;
            }
            else
            {
                dataTable = await JsonToDataTable(componentModel, httpContext);

                if (componentModel.Cache)
                {
                    _memoryCache.Set(componentModel.Id, dataTable, GetCacheOptions());
                }

                return dataTable;
            }
        }

        private async Task<DataTable> JsonToDataTable(ComponentModel componentModel, HttpContext httpContext)
        {
            string json = string.Empty;

            if (string.IsNullOrEmpty(componentModel.Url))
            {
                json = componentModel.JSON;
            }
            else
            {
                if (TextHelper.IsAbsolutePath(componentModel.Url))
                {
                    json = File.ReadAllText(componentModel.Url);
                }
                else
                {
                    var url = componentModel.Url;
                    if (url.StartsWith("/"))
                    {
                        url = url.Substring(1);
                    }

                    if (url.StartsWith("http") == false)
                    {
                        url = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}";
                    }

                    string token = string.Empty;

                    _httpClient.DefaultRequestHeaders.Clear();

                    if (!string.IsNullOrEmpty(token))
                    {
                        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                    }

                    if (componentModel is GridModel gridModel)
                    {
                        foreach (var key in gridModel.ApiRequestHeaders.Keys)
                        {
                            _httpClient.DefaultRequestHeaders.Add(key, gridModel.ApiRequestHeaders[key]);
                        }
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
                dataTable = Tabulate(json, componentModel);
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to convert JSON to a table. Please ensure root element is an array", ex);
            }

            if (componentModel.GetColumns().Any())
            {
                string[] selectedColumns = componentModel.GetColumns().Select(c => c.Expression).ToArray();
                dataTable = new DataView(dataTable).ToTable(false, selectedColumns);
            }

            return dataTable;
        }

        private DataTable Tabulate(string json, ComponentModel componentModel)
        {
            JToken? jToken = JToken.Parse(json);

            if (componentModel is GridModel gridModel && string.IsNullOrEmpty(gridModel.JsonArrayProperty) == false)
            {
                jToken = jToken.SelectToken(gridModel.JsonArrayProperty);
            }

            if (jToken is not JArray)
            {
                foreach (JToken child in jToken.Children())
                {
                    if (child.First is JArray)
                    {
                        jToken = child.First;
                        break;
                    }   
                }
            }

            Dictionary<string,Type> DataTypes = new Dictionary<string, Type>();
            if (jToken is JArray srcArray)
            {
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
                        else
                        {
                            cleanRow.Add(column.Name, column.Value.ToString(Formatting.None));
                            DataTypes[column.Name] = typeof(JsonDocument);
                        }
                    }

                    trgArray.Add(cleanRow);
                }

                DataTable datatable = JsonConvert.DeserializeObject<DataTable>(trgArray.ToString()) ?? new DataTable();

                foreach (string columnName in DataTypes.Keys)
                {
                    if (datatable.Columns.Contains(columnName))
                    {
                        datatable.Columns[columnName]!.ExtendedProperties.Add("DataType", DataTypes[columnName]);
                    }
                }

                return datatable;
            }

            return new DataTable();
        }
    }
}
