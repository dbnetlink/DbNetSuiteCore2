using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Repositories;

namespace DbNetSuiteCore.Factories.Interfaces
{
    public interface IRepositoryFactory
    {
        ISqlRepository GetSqlRepository(DataSourceType dataSourceType);
    }
}
