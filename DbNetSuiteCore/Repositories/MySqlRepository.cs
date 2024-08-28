namespace DbNetSuiteCore.Repositories
{
    public class MySqlRepository : DbRepository, IMySqlRepository
    {
        public MySqlRepository(IConfiguration configuration, IWebHostEnvironment env) : base(Enums.DataSourceType.MySql, configuration, env)
        {
        }
    }
}
