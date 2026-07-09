using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface IDuckDbInMemoryRepository
    {
        IDbConnection CreateConnection();
    }
}