using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Repositories;

namespace DbNetSuiteCore.Factories.Interfaces
{
    /// <summary>
    /// Factory for creating repository instances based on data source type.
    /// This abstraction reduces coupling and simplifies adding new data source types.
    /// </summary>
    public interface IRepositoryFactory
    {
        /// <summary>
        /// Gets the appropriate SQL repository for the specified data source type.
        /// </summary>
        /// <param name="dataSourceType">The type of data source (MSSQL, SQLite, MySQL, etc.)</param>
        /// <returns>An instance of ISqlRepository for the specified data source</returns>
        /// <exception cref="ArgumentException">Thrown when the data source type is not supported</exception>
        ISqlRepository GetSqlRepository(DataSourceType dataSourceType);

        /// <summary>
        /// Gets the JSON repository instance.
        /// </summary>
        IJSONRepository GetJsonRepository();

        /// <summary>
        /// Gets the Excel repository instance.
        /// </summary>
        IExcelRepository GetExcelRepository();

        /// <summary>
        /// Gets the FileSystem repository instance.
        /// </summary>
        IFileSystemRepository GetFileSystemRepository();
    }
}
