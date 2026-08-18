using DbNetSuiteCore.Models;
using DbNetSuiteCore.Enums;
using System.Data;
using Microsoft.Extensions.FileProviders;
using DbNetSuiteCore.Helpers;
using Microsoft.Extensions.Caching.Memory;

namespace DbNetSuiteCore.Repositories
{
    public class FileSystemRepository : DuckDbInMemoryRepository, IFileSystemRepository
    {
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _memoryCache;
        public FileSystemRepository(IConfiguration configuration, IWebHostEnvironment env, IMemoryCache memoryCache) : base(configuration, env, DataSourceType.FileSystem)
        {
            _env = env;
            _memoryCache = memoryCache;
        }

        public override async Task CreateTable(ComponentModel componentModel, IDbConnection connection)
        {
            string jsonFilePath = await WriteJsonFile(ContentsAsJson(componentModel.Url), componentModel, _memoryCache);
            CreateTableFromJson(jsonFilePath, componentModel, connection);
        }

        public static void UpdateUrl(ComponentModel componentModel)
        {
            var folderSeparator = "/";

            if (TextHelper.IsAbsolutePath(componentModel.Url))
            {
                folderSeparator = Path.DirectorySeparatorChar.ToString();
            }

            var urlParts = componentModel.Url.Split(folderSeparator);

            if (string.IsNullOrEmpty(componentModel.ParentModel?.Name) == false)
            {
                var url = componentModel.ParentModel?.Name ?? string.Empty;
                urlParts = urlParts.Append(url).ToArray();

                componentModel.Url = string.Join(folderSeparator, urlParts.ToArray());
            }
        }


        private string ConvertUrlToFilePath(string url)
        {
            if (TextHelper.IsAbsolutePath(url))
            {
                return url;
            }

            var pathParts = _env.WebRootPath.Split(Path.DirectorySeparatorChar.ToString());
            var urlParts = url.Split("/");

            foreach (var part in urlParts)
            {
                if (part == "..")
                {
                    pathParts = pathParts.Take(pathParts.Count() - 1).ToArray();
                }
                else
                {
                    pathParts = pathParts.Append(part).ToArray();
                }
            }

            return string.Join(Path.DirectorySeparatorChar.ToString(), pathParts);
        }


        private string ContentsAsJson(string path)
        {
            path = ConvertUrlToFilePath(path);
            var provider = new PhysicalFileProvider(path);
            return System.Text.Json.JsonSerializer.Serialize(Transform(provider.GetDirectoryContents(string.Empty)));
        }

        private IEnumerable<Models.FileSystemInfo> Transform(IDirectoryContents directoryContents)
        {
            foreach (IFileInfo file in directoryContents)
            {
                yield return new Models.FileSystemInfo(file, _env);
            }
        }
    }
}
