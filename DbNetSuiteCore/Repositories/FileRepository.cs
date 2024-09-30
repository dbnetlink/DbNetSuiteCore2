using DbNetSuiteCore.Models;
using Microsoft.Extensions.Caching.Memory;

namespace DbNetSuiteCore.Repositories
{
    public class FileRepository
    { 
        protected MemoryCacheEntryOptions GetCacheOptions(GridModel gridModel)
        {
            return new MemoryCacheEntryOptions()
                       .SetSlidingExpiration(TimeSpan.FromMinutes(1))
                       .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                       .SetPriority(CacheItemPriority.Normal)
                       .SetSize(1024);
        }
    }

}
