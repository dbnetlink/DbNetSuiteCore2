using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public interface IDuckDbInMemoryRepository
    {
        IDbConnection CreateConnection();
        Task CreateTable(ComponentModel componentModel, IDbConnection connection);
    }
}