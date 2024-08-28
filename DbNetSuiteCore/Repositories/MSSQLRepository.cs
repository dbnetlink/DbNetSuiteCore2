namespace DbNetSuiteCore.Repositories
{
    public class MSSQLRepository : DbRepository, IMSSQLRepository
    {
        public MSSQLRepository(IConfiguration configuration, IWebHostEnvironment env) : base(Enums.DataSourceType.MSSQL, configuration, env)
        {
        }
    }
}
