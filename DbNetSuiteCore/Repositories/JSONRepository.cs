using DbNetSuiteCore.Constants;
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
    public class JSONRepository : DuckDbInMemoryRepository, IJSONRepository, IDuckDbInMemoryRepository
    {
        private readonly IMemoryCache _memoryCache;
        private static readonly HttpClient _httpClient = new HttpClient();

        public JSONRepository(IConfiguration configuration, IWebHostEnvironment env, IMemoryCache memoryCache) : base(configuration, env, Enums.DataSourceType.JSON)
        {
            _memoryCache = memoryCache;
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
                    url = RequestHelper.BuildUrl(componentModel.HttpContext.Request, url.Split("?").First(), url.Split("?").Length > 1 ? url.Split("?").Last() : null);
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
                if (IsMemoryCacheEnabled(componentModel))
                {
                    var guid = Guid.NewGuid();
                    _memoryCache.Set(guid, json, CacheHelper.GetShortExpiryCacheOptions());
                    return RequestHelper.BuildUrl(componentModel.HttpContext.Request, $"/{PageNames.JsonCache}{Middleware.DbNetSuiteCore.Extension}", $"key={guid}");
                }

                var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
                await File.WriteAllTextAsync(tempPath, json);
                return tempPath;
            }
        }

        private bool IsMemoryCacheEnabled(ComponentModel componentModel)
        {
            return (componentModel is GridSelectModel gridSelectModel && gridSelectModel.JsonCacheType == CacheType.Memory);
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

        public override async Task CreateTable(ComponentModel componentModel, IDbConnection connection)
        {
            if (this is not IJSONRepository jSONRepository)
            {
                throw new NotImplementedException("The current repository does not support JSON data source.");
            }

            var jsonFilePath = await jSONRepository.JsonFromUrl(componentModel);

            using (var cmd = connection.CreateCommand())
            {
                if (string.IsNullOrEmpty(jsonFilePath))
                {
                    cmd.CommandText = $"CREATE TABLE {componentModel.TableName} ({string.Join(", ", componentModel.GetColumns().Select(c => $"{c.Expression} varchar"))})";
                }
                else
                {
                    cmd.CommandText = $"CREATE TABLE {componentModel.TableName} AS FROM read_json('{jsonFilePath}')";
                }
                cmd.ExecuteNonQuery();
            }

            if (!IsMemoryCacheEnabled(componentModel))
            {
                try
                {
                    File.Delete(jsonFilePath);
                }
                catch
                {
                }
            }
        }
    }
}
