using DbNetSuiteCore.Middleware;
using Microsoft.Extensions.Options;

namespace DbNetSuiteCore.Services.Interfaces
{
    public interface IComponentService
    {
        Task<Byte[]> Process(HttpContext context, string page, IOptions<DbNetSuiteCoreOptions>? options = null);
    }
}