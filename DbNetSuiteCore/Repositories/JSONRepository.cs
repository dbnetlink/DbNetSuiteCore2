using DbNetSuiteCore.Constants;
using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using DuckDB.NET.Data;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Text.Json;
using System.Web;

namespace DbNetSuiteCore.Repositories
{
    public class JSONRepository : DbRepository, IJSONRepository, IDuckDbInMemoryRepository
    {
        private readonly DuckDBConnection _root;
        private readonly object _lock = new();
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly IMemoryCache _memoryCache;

        public JSONRepository(IConfiguration configuration, IWebHostEnvironment env, IMemoryCache memoryCache) : base(Enums.DataSourceType.JSON, configuration, env)
        {
            _memoryCache = memoryCache;
            _root = new DuckDBConnection("DataSource=:memory:");
            _root.Open();
        }
        public IDbConnection CreateConnection()
        {
            lock (_lock)
            {
                var conn = _root.Duplicate();
                conn.Open();
                return conn;
            }
        }

        public async Task<string> JsonFromUrl(ComponentModel componentModel)
        {
            if (componentModel is not GridSelectModel gridSelectModel)
            {
                return string.Empty;
            }

            if (componentModel.TriggerName == TriggerNames.ApiRequestParameters)
            {
                _memoryCache.Remove(gridSelectModel.CacheKey);
            }
            else if (_memoryCache.TryGetValue(gridSelectModel.CacheKey, out string cachedJson))
            {
                if (cachedJson != null)
                {
                    return await WriteFile(cachedJson);
                }
            }

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

                if (url.StartsWith("/") && componentModel.HttpContext != null)
                {
                    url = url.Substring(1);
                    url = $"{componentModel.HttpContext.Request.Scheme}://{componentModel.HttpContext.Request.Host}/{url}";
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

                if (gridSelectModel.ApiRequestParameters.Keys.Any())
                {
                    url = UpdateUrlParameters(url, gridSelectModel.ApiRequestParameters);
                }

                foreach (var key in gridSelectModel.ApiRequestHeaders.Keys)
                {
                    _httpClient.DefaultRequestHeaders.Add(key, gridSelectModel.ApiRequestHeaders[key]);
                }

                json = await _httpClient.GetStringAsync(url);

                if (componentModel is GridModel gridModel && String.IsNullOrEmpty(gridModel.JsonTransformPluginName) == false && componentModel.HttpContext != null)
                {
                    IEnumerable<object> items = (IEnumerable<object>)PluginHelper.TransformJson(gridModel, json);

                    if (string.IsNullOrEmpty(gridModel.Message) == false)
                    {
                        throw new Exception(gridModel.Message);
                    }

                    json = JsonConvert.SerializeObject(items.ToList());
                }
            }

            if (json == "[]")
            {
                return string.Empty;
            }

            json = Tabulate(json, componentModel);


            if (gridSelectModel.Cache)
            {
                _memoryCache.Set(gridSelectModel.CacheKey, json, CacheHelper.GetCacheOptions());
            }

            return await WriteFile(json);

            async Task<string> WriteFile(string json)
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
                await File.WriteAllTextAsync(tempPath, json);
                return tempPath;
            }

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
                        urlParameters.Add($"{key}={HttpUtility.UrlDecode(apiParameters[key])}");
                    }
                }
                if (urlParameters.Count > 0)
                {
                    url = $"{url.Split("?").First()}?{string.Join("&", urlParameters)}";
                }
            }
            return url;
        }

        private string Tabulate(string json, ComponentModel componentModel)
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

            if (jToken is not JArray srcArray)
            {
                throw new Exception("JSON array not found.");
            }

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

            return trgArray.ToString();
        }

        public void Dispose() => _root.Dispose();
    }
}
