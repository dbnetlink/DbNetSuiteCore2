namespace DbNetSuiteCore.Repositories
{
    public class PostgreSqlRepository : DbRepository, IPostgreSqlRepository
    {
        public PostgreSqlRepository(IConfiguration configuration, IWebHostEnvironment env) : base(Enums.DataSourceType.PostgreSql, configuration, env)
        {
        }
    }
}
