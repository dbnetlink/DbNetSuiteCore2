namespace DbNetSuiteCore.Repositories
{
    public class DuckDbRepository : DbRepository, IDuckDbRepository
    {
        public DuckDbRepository(IConfiguration configuration, IWebHostEnvironment env) : base(Enums.DataSourceType.DuckDB, configuration, env)
        {
        }

    }
}
