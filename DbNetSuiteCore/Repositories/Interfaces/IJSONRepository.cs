using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.Repositories
{
    public interface IJSONRepository : ISqlRepository
    {
        Task<string> JsonFromUrl(ComponentModel componentModel);
    }
}