using DbNetSuiteCore.Constants;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Text.Json;
using System.Web;

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
        public async Task GetRecords(GridSelectModel gridSelectModel, HttpContext httpContext)
        {
            gridSelectModel.Data = await BuildDataTable(gridSelectModel, httpContext);

            var dataTable = gridSelectModel.Data;
            if (gridSelectModel is GridModel gridModel)
            {
                if (gridModel.Data.Rows.Count > 0)
                {
                    dataTable.FilterAndSort(gridModel);
                    gridModel.ConvertEnumLookups();
                    gridModel.GetDistinctLookups();
                }
            }

            if (gridSelectModel is SelectModel selectModel)
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
        public async Task GetRecord(GridSelectModel gridSelectModel, HttpContext httpContext)
        {
            var dataTable = await BuildDataTable(gridSelectModel, httpContext);
            dataTable.FilterWithPrimaryKey(gridSelectModel);
            gridSelectModel.ConvertEnumLookups();
        }

        public async Task<DataTable> GetColumns(GridSelectModel gridSelectModel, HttpContext httpContext)
        {
            gridSelectModel.Data = await BuildDataTable(gridSelectModel, httpContext);
            return gridSelectModel.Data;
        }

        public void UpdateApiRequestParameters(GridSelectModel gridSelectModel, HttpContext context)
        {
            Dictionary<string, string> apiRequestParameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(RequestHelper.FormValue(TriggerNames.ApiRequestParameters, string.Empty, context)) ?? new Dictionary<string, string>();

            apiRequestParameters = new Dictionary<string, string>(apiRequestParameters, StringComparer.OrdinalIgnoreCase);
            foreach (string key in gridSelectModel.ApiRequestParameters.Keys)
            {
                if (apiRequestParameters.ContainsKey(key))
                {
                    gridSelectModel.ApiRequestParameters[key] = HttpUtility.UrlEncode(apiRequestParameters[key]);
                }
            }
        }

        private async Task<DataTable> BuildDataTable(GridSelectModel gridSelectModel, HttpContext httpContext)
        {
            if (gridSelectModel.Cache)
            {
                if (gridSelectModel.TriggerName == TriggerNames.ApiRequestParameters)
                {
                    _memoryCache.Remove(gridSelectModel.CacheKey);
                }
                else if (_memoryCache.TryGetValue(gridSelectModel.CacheKey, out DataTable cachedDataTable))
                {
                    if (cachedDataTable != null)
                    {
                        return cachedDataTable;
                    }
                }
            }

            DataTable dataTable = await JsonToDataTable(gridSelectModel, httpContext);

            if (gridSelectModel.Cache)
            {
                _memoryCache.Set(gridSelectModel.CacheKey, dataTable, GetCacheOptions());
            }

            return dataTable;
        }

        private async Task<DataTable> JsonToDataTable(ComponentModel componentModel, HttpContext httpContext)
        {
            string json = string.Empty;

            if (String.IsNullOrEmpty(componentModel.DataSourcePluginName) == false)
            {
                json = (string)PluginHelper.InvokeMethod(componentModel.DataSourcePluginName, nameof(IDataSourcePlugin.GetData), componentModel, null, false);
            }
            else if (string.IsNullOrEmpty(componentModel.Url))
            {
                json = componentModel.JSON;
            }
            else
            {
                var url = componentModel.Url;

                if (url.StartsWith("/") && httpContext != null)
                {
                    url = url.Substring(1);
                    url = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}";
                }

                if (Uri.IsWellFormedUriString(url, UriKind.Absolute) == false)
                {
                    throw new Exception($"Url is not well formed => {url}");
                }

                string token = string.Empty;

                _httpClient.DefaultRequestHeaders.Clear();

                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                }

                if (componentModel is GridSelectModel gridSelectModel)
                {
                    if (gridSelectModel.ApiRequestParameters.Keys.Any())
                    {
                        url = UpdateUrlParameters(url, gridSelectModel.ApiRequestParameters);
                    }

                    foreach (var key in gridSelectModel.ApiRequestHeaders.Keys)
                    {
                        _httpClient.DefaultRequestHeaders.Add(key, gridSelectModel.ApiRequestHeaders[key]);
                    }
                }

                json = await _httpClient.GetStringAsync(url);
            }

            if (componentModel is GridModel gridModel && String.IsNullOrEmpty(gridModel.JsonTransformPluginName) == false && httpContext != null)
            {
                json = PluginHelper.TransformJson(gridModel, json);

                if (string.IsNullOrEmpty(gridModel.Message) == false)
                {
                    throw new Exception(gridModel.Message);
                }
            }

            DataTable dataTable = new();
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

        private string UpdateUrlParameters(string url, Dictionary<string, string> apiParameters)
        {
            if (apiParameters.Keys.Any())
            {
                var urlParameters = new List<string>();
                if (url.Split("?").Length > 1)
                {
                    urlParameters = url.Split("?").Last().Split("&").ToList();
                }
                foreach (var key in apiParameters.Keys)
                {
                    if (string.IsNullOrEmpty(apiParameters[key]) == false)
                    {
                        urlParameters.Add($"{key}={apiParameters[key]}");
                    }
                }
                if (urlParameters.Count > 0)
                {
                    url = $"{url.Split("?").First()}?{string.Join("&", urlParameters)}";
                }
            }
            return url;
        }

        private DataTable Tabulate(string json, ComponentModel componentModel)
        {
            JToken jToken = JToken.Parse(json);

            if (componentModel is GridModel gridModel && string.IsNullOrEmpty(gridModel.JsonArrayProperty) == false)
            {
                jToken = jToken.SelectToken(gridModel.JsonArrayProperty);
            }

            if (jToken is not JArray && jToken != null)
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

            Dictionary<string, Type> dataTypes = new Dictionary<string, Type>();
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

                JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
                //  jsonSerializerSettings.MaxDepth = 10;

                DataTable dataTable = JsonConvert.DeserializeObject<DataTable>(trgArray.ToString(), jsonSerializerSettings) ?? new DataTable();

                foreach (string columnName in dataTypes.Keys)
                {
                    if (dataTable.Columns.Contains(columnName))
                    {
                        dataTable.Columns[columnName]!.ExtendedProperties.Add("DataType", dataTypes[columnName]);
                    }
                }

                AddMissingColumns(componentModel, dataTable, dataColumnNames);

                return dataTable;
            }

            return new DataTable();
        }

        private void AddMissingColumns(ComponentModel componentModel, DataTable dataTable, List<string> dataColumnNames)
        {
            if (componentModel.GetColumns().Any())
            {
                List<string> columnNames = componentModel.GetColumns().Select(c => c.Expression).ToList();
                List<string> missingColumnNames = columnNames.Where(c => dataColumnNames.Contains(c, StringComparer.OrdinalIgnoreCase) == false).ToList();

                foreach (string columnName in missingColumnNames)
                {
                    DataColumn Col = dataTable.Columns.Add(columnName, typeof(string));
                    Col.SetOrdinal(columnNames.IndexOf(columnName));
                }

                dataTable = new DataView(dataTable).ToTable(false, columnNames.ToArray());
            }
        }

        // Experimental conversion of JSON to DataTable for System.Text.Json
        /* 
        private DataTable SystemTextJsonTabulate(string json, ComponentModel componentModel)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;

            if (componentModel is GridModel gridModel && string.IsNullOrEmpty(gridModel.JsonArrayProperty) == false)
            {
                root = root.GetProperty(gridModel.JsonArrayProperty);
            }

            if (root.ValueKind != JsonValueKind.Array)
            {
                foreach (JsonProperty property in root.EnumerateObject())
                {

                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        root = property.Value;
                        break;
                    }
                }
            }

            Dictionary<string, Type> dataTypes = new Dictionary<string, Type>();
            List<string> dataColumnNames = new List<string>();
            JsonArray jsonArray = new JsonArray();

            if (root.ValueKind != JsonValueKind.Array)
            {
                return new DataTable();
            }
            foreach (JsonElement jsonElement in root.EnumerateArray())
            {
                JsonNode? jsonNode = JsonNode.Parse(jsonElement.GetRawText());
                if (jsonNode is JsonObject row)
                {
                    var cleanRow = new JsonObject();

                    foreach (var kvp in row)
                    {
                        var columnName = kvp.Key;
                        var columnValue = kvp.Value;

                        cleanRow[columnName] = CopyJsonNode(columnValue);

                        if (columnValue is not JsonValue)
                        {
                            dataTypes[columnName] = typeof(JsonDocument);
                        }

                        if (!dataColumnNames.Contains(columnName))
                        {
                            dataColumnNames.Add(columnName);
                        }
                    }

                    jsonArray.Add(cleanRow);
                }
            }

            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
            //  jsonSerializerSettings.MaxDepth = 10;

            DataTable dataTable = new DataTable();

            foreach (string name in dataColumnNames)
            {
                dataTable.Columns.Add(new DataColumn(name));
            }

            foreach (JsonElement jsonObject in root.EnumerateArray())
            {
                DataRow newRow = dataTable.NewRow();
                foreach (var property in jsonObject.EnumerateObject())
                {
                    if (dataTable.Columns.Contains(property.Name))
                    {
                        // Convert the JsonElement's value to a string for the DataRow
                        newRow[property.Name] = property.Value.ToString();
                    }
                }
                dataTable.Rows.Add(newRow);
            }

            AddMissingColumns(componentModel, dataTable, dataColumnNames);

            return dataTable;

        }

        private JsonNode? CopyJsonNode(JsonNode? value)
        {
            if (value == null)
            {
                return null;
            }
            if (value is JsonValue)
            {
                return JsonValue.Create(value.GetValue<object>());
            }
            else
            {
                return JsonNode.Parse(value.ToJsonString());
            }
        }
        */
    }
}
