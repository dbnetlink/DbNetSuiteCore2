using DbNetSuiteCore.Constants;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DuckDB.NET.Data;
using Microsoft.Extensions.Caching.Memory;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public class DuckDbInMemoryRepository : DbRepository, IDuckDbInMemoryRepository
    {
        private readonly DuckDBConnection _root;
        private readonly object _lock = new();

        public DuckDbInMemoryRepository(IConfiguration configuration, IWebHostEnvironment env, DataSourceType dataSourceType) : base(dataSourceType, configuration, env)
        {
            _root = new DuckDBConnection("DataSource=:memory:");
            _root.Open();
        }
        public IDbConnection CreateConnection()
        {
            lock (_lock)
            {
                var conn = (DuckDBConnection)_root.Duplicate();
                conn.Open();
                return conn;
            }
        }

        protected bool IsMemoryCacheEnabled(ComponentModel componentModel)
        {
            return (componentModel is GridSelectModel gridSelectModel && gridSelectModel.JsonCacheType == CacheType.Memory);
        }

        public async Task<string> WriteJsonFile(string json, ComponentModel componentModel, IMemoryCache memoryCache)
        {
            if (IsMemoryCacheEnabled(componentModel))
            {
                var guid = Guid.NewGuid();
                memoryCache.Set(guid, json, CacheHelper.GetShortExpiryCacheOptions());
                return RequestHelper.BuildUrl(componentModel.HttpContext.Request, $"/{PageNames.JsonCache}{Middleware.DbNetSuiteCore.Extension}", $"key={guid}");
            }

            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
            await File.WriteAllTextAsync(tempPath, json);
            return tempPath;
        }

        public virtual async Task CreateTable(ComponentModel componentModel, IDbConnection connection)
        {
            throw new NotImplementedException("CreateTable method must be implemented in the derived class.");
        }

        protected void CreateTableFromJson(string jsonPath, ComponentModel componentModel, IDbConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                if (string.IsNullOrEmpty(jsonPath))
                {
                    cmd.CommandText = $"CREATE TABLE {componentModel.TableName} ({string.Join(", ", componentModel.GetColumns().Select(c => $"{c.Expression} varchar"))})";
                }
                else
                {
                    cmd.CommandText = $"CREATE TABLE {componentModel.TableName} AS FROM read_json('{jsonPath}')";
                }
                cmd.ExecuteNonQuery();
            }

            if (!IsMemoryCacheEnabled(componentModel))
            {
                try
                {
                    File.Delete(jsonPath);
                }
                catch
                {
                }
            }
        }

        public void Dispose() => _root.Dispose();
    }
}