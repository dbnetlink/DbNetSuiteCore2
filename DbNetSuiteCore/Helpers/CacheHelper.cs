using DbNetSuiteCore.Models;
using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace DbNetSuiteCore.Helpers
{

    public static class CacheHelper
    {

        public static string CacheModel(ComponentModel componentModel)
        {
            return CacheObject(componentModel, componentModel.Id, componentModel.HttpContext);
        }

        public static string CacheSummaryModel(SummaryModel summaryModel, ComponentModel componentModel)
        {
            return CacheObject(summaryModel, $"{componentModel.Id}Summary", componentModel.HttpContext);
        }

        private static string CacheObject(object obj, string key, HttpContext httpContext)
        {
            string serialisedModel = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            serialisedModel = TextHelper.Compress(serialisedModel);

            IMemoryCache? memoryCache = httpContext.RequestServices.GetService<IMemoryCache>();

            if (memoryCache == null)
            {
                throw new Exception("MemoryCache service is not available.");
            }
            var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(30));
            memoryCache.Set(key, serialisedModel, cacheEntryOptions);
            return TextHelper.ObfuscateString(key);
        }

        public static string GetModel(string cacheKey, HttpContext httpContext)
        {
            if (string.IsNullOrEmpty(cacheKey))
            {
                return string.Empty;
            }   
            cacheKey = TextHelper.DeobfuscateString(cacheKey);

            IMemoryCache? memoryCache = httpContext.RequestServices.GetService<IMemoryCache>();

            if (memoryCache == null)
            {
                throw new Exception("MemoryCache service is not available.");
            }
            if (memoryCache.TryGetValue(cacheKey, out object? serialisedModelObj) && serialisedModelObj is string serialisedModel)
            {
                return TextHelper.Decompress(serialisedModel);
            }
            throw new Exception("Cached model not found.");
        }
    }
}