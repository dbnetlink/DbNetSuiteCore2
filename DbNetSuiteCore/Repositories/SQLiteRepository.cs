namespace DbNetSuiteCore.Repositories
{
    public class SQLiteRepository : DbRepository, ISQLiteRepository
    {
        public SQLiteRepository(IConfiguration configuration, IWebHostEnvironment env) : base(Enums.DataSourceType.SQLite, configuration, env)
        {
        }

    }
}
