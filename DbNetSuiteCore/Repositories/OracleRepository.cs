namespace DbNetSuiteCore.Repositories
{
    public class OracleRepository : DbRepository, IOracleRepository
    {
        public OracleRepository(IConfiguration configuration, IWebHostEnvironment env) : base(Enums.DataSourceType.Oracle, configuration, env)
        {
        }
    }
}