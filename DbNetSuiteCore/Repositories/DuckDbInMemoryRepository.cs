using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Models;
using DuckDB.NET.Data;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public class DuckDbInMemoryRepository : DbRepository, IDuckDbInMemoryRepository
    {
        private readonly DuckDBConnection _root;
        private readonly object _lock = new();

        public DuckDbInMemoryRepository(IConfiguration configuration, IWebHostEnvironment env, DataSourceType dataSourceType) : base(dataSourceType, configuration, env)
        {
            _root = new DuckDBConnection("DataSource=:memory:");
            _root.Open();
        }
        public IDbConnection CreateConnection()
        {
            lock (_lock)
            {
                var conn = (DuckDBConnection)_root.Duplicate();
                conn.Open();
                return conn;
            }
        }
        public virtual async Task CreateTable(ComponentModel componentModel, IDbConnection connection)
        {
            throw new NotImplementedException("CreateTable method must be implemented in the derived class.");
        }

        public void Dispose() => _root.Dispose();
    }
}