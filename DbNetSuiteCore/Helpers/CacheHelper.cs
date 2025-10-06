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
            string serialisedModel = Newtonsoft.Json.JsonConvert.SerializeObject(componentModel);
            serialisedModel = TextHelper.Compress(serialisedModel);

            IMemoryCache? memoryCache = componentModel.HttpContext.RequestServices.GetService<IMemoryCache>();

            if (memoryCache == null)
            {
                throw new Exception("MemoryCache service is not available.");
            }
            var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(30));
            memoryCache.Set(componentModel.Id, serialisedModel, cacheEntryOptions);

            return TextHelper.ObfuscateString(componentModel.Id);
        }


        public static string GetModel(string cacheKey, HttpContext httpContext)
        {
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