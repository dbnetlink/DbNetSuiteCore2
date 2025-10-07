using DbNetSuiteCore.Helpers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;

namespace DbNetSuiteCore.Services
{
   public interface ICacheService
    {
        Task SetAsync(string key, string value);
        Task<string?> GetAsync(string key);
    }

    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
  
        public CacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task SetAsync(string key, string value)
        {
            var options = new DistributedCacheEntryOptions();
            options.SetSlidingExpiration(TimeSpan.FromMinutes(30));

            await _cache.SetStringAsync(key, value, options);
        }

        public async Task<string?> GetAsync(string key)
        {
            return await _cache.GetStringAsync(key);
        }
    }
}

