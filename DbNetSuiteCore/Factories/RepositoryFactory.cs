using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Factories.Interfaces;
using DbNetSuiteCore.Repositories;

#nullable enable

namespace DbNetSuiteCore.Factories
{
    /// <summary>
    /// Factory implementation for creating repository instances.
    /// Uses dependency injection to provide pre-configured repository instances.
    /// </summary>
    public class RepositoryFactory : IRepositoryFactory
    {
        private readonly IMSSQLRepository _msSqlRepository;
        private readonly ISQLiteRepository _sqliteRepository;
        private readonly IJSONRepository _jsonRepository;
        private readonly IFileSystemRepository _fileSystemRepository;
        private readonly IMySqlRepository _mySqlRepository;
        private readonly IPostgreSqlRepository _postgreSqlRepository;
        private readonly IExcelRepository _excelRepository;
        private readonly IOracleRepository _oracleRepository;
        private readonly IDuckDbRepository _duckDbRepository;
        private readonly ILogger<RepositoryFactory>? _logger;

        public RepositoryFactory(
            IMSSQLRepository msSqlRepository,
            ISQLiteRepository sqliteRepository,
            IJSONRepository jsonRepository,
            IFileSystemRepository fileSystemRepository,
            IMySqlRepository mySqlRepository,
            IPostgreSqlRepository postgreSqlRepository,
            IExcelRepository excelRepository,
            IOracleRepository oracleRepository,
            IDuckDbRepository duckDbRepository,
            ILogger<RepositoryFactory>? logger = null)
        {
            _msSqlRepository = msSqlRepository ?? throw new ArgumentNullException(nameof(msSqlRepository));
            _sqliteRepository = sqliteRepository ?? throw new ArgumentNullException(nameof(sqliteRepository));
            _jsonRepository = jsonRepository ?? throw new ArgumentNullException(nameof(jsonRepository));
            _fileSystemRepository = fileSystemRepository ?? throw new ArgumentNullException(nameof(fileSystemRepository));
            _mySqlRepository = mySqlRepository ?? throw new ArgumentNullException(nameof(mySqlRepository));
            _postgreSqlRepository = postgreSqlRepository ?? throw new ArgumentNullException(nameof(postgreSqlRepository));
            _excelRepository = excelRepository ?? throw new ArgumentNullException(nameof(excelRepository));
            _oracleRepository = oracleRepository ?? throw new ArgumentNullException(nameof(oracleRepository));
            _duckDbRepository = duckDbRepository ?? throw new ArgumentNullException(nameof(duckDbRepository));
            _logger = logger;
        }

        /// <inheritdoc/>
        public ISqlRepository GetSqlRepository(DataSourceType dataSourceType)
        {
            _logger?.LogDebug("Creating repository for data source type: {DataSourceType}", dataSourceType);

            return dataSourceType switch
            {
                DataSourceType.MSSQL => _msSqlRepository,
                DataSourceType.SQLite => _sqliteRepository,
                DataSourceType.MySql => _mySqlRepository,
                DataSourceType.PostgreSql => _postgreSqlRepository,
                DataSourceType.Oracle => _oracleRepository,
                DataSourceType.DuckDB => _duckDbRepository,
                DataSourceType.Excel => _excelRepository,

                DataSourceType.JSON or DataSourceType.FileSystem => 
                    throw new InvalidOperationException(
                        $"Data source type '{dataSourceType}' is not a SQL repository. " +
                        $"Use GetJsonRepository(), GetExcelRepository(), or GetFileSystemRepository() instead."),
                _ => throw new ArgumentException(
                    $"Unsupported data source type: {dataSourceType}. " +
                    $"Supported SQL types are: MSSQL, SQLite, MySql, PostgreSql, Oracle.",
                    nameof(dataSourceType))
            };
        }

        /// <inheritdoc/>
        public IJSONRepository GetJsonRepository() => _jsonRepository;

        /// <inheritdoc/>
        public IFileSystemRepository GetFileSystemRepository() => _fileSystemRepository;
    }
}
