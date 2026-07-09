using DuckDB.NET.Data;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public class ExcelRepository : DbRepository, IExcelRepository, IDuckDbInMemoryRepository
    {
        private readonly DuckDBConnection _root;
        private readonly object _lock = new();

        public ExcelRepository(IConfiguration configuration, IWebHostEnvironment env) : base(Enums.DataSourceType.Excel, configuration, env)
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

        public void Dispose() => _root.Dispose();
    }
}