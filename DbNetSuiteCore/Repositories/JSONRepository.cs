using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DocumentFormat.OpenXml.InkML;
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
            componentModel.Data = await BuildDataTable(componentModel, httpContext);

            var dataTable = componentModel.Data;
            if (componentModel is GridModel gridModel)
            {
                if (gridModel.Data.Rows.Count > 0)
                {
                    dataTable.FilterAndSort(gridModel);
                    gridModel.ConvertEnumLookups();
                    gridModel.GetDistinctLookups();
                }
            }

            if (componentModel is SelectModel selectModel)
            {
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


        public void UpdateApiRequestParameters(GridModel gridModel, HttpContext? context)
        {
            Dictionary<string, string> apiRequestParameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(RequestHelper.FormValue("apiRequestParameters", string.Empty, context)) ?? new Dictionary<string, string>();
            apiRequestParameters = new Dictionary<string, string>(apiRequestParameters, StringComparer.OrdinalIgnoreCase);
            foreach (string key in gridModel.ApiRequestParameters.Keys)
            {
                if (apiRequestParameters.ContainsKey(key))
                {
                    gridModel.ApiRequestParameters[key] = apiRequestParameters[key];
                }
            }
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
                        var parameters = new List<string>();
                        if (url.Split("?").Length > 1)
                        {
                            parameters = url.Split("?").Last().Split("&").ToList();
                        }
                        
                        foreach (var key in gridModel.ApiRequestParameters.Keys)
                        {
                            if (string.IsNullOrEmpty(gridModel.ApiRequestParameters[key]) == false)
                            {
                                parameters.Add($"{key}={gridModel.ApiRequestParameters[key]}");
                            }
                        }

                        if (parameters.Count > 0)
                        {
                            url = $"{url.Split("?").First()}?{string.Join("&", parameters)}";
                        }   

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

            Dictionary<string,Type> dataTypes = new Dictionary<string, Type>();
            List<string> dataColumnNames = new List<string>();

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
                            dataTypes[column.Name] = typeof(JsonDocument);
                        }

                        if (!dataColumnNames.Contains(column.Name))
                        {
                            dataColumnNames.Add(column.Name);
                        }
                    }

                    trgArray.Add(cleanRow);
                }

                DataTable dataTable = JsonConvert.DeserializeObject<DataTable>(trgArray.ToString()) ?? new DataTable();

                foreach (string columnName in dataTypes.Keys)
                {
                    if (dataTable.Columns.Contains(columnName))
                    {
                        dataTable.Columns[columnName]!.ExtendedProperties.Add("DataType", dataTypes[columnName]);
                    }
                }

                if (componentModel.GetColumns().Any())
                {
                    List<string> columnNames = componentModel.GetColumns().Select(c => c.Expression).ToList();
                    List<string> missingColumnNames = columnNames.Where(c => dataColumnNames.Contains(c) == false).ToList();

                    foreach (string columnName in missingColumnNames)
                    {
                        DataColumn Col = dataTable.Columns.Add(columnName, typeof(string));
                        Col.SetOrdinal(columnNames.IndexOf(columnName));
                    }

                    dataTable = new DataView(dataTable).ToTable(false, columnNames.ToArray());
                }

                return dataTable;
            }

            return new DataTable();
        }
    }
}
