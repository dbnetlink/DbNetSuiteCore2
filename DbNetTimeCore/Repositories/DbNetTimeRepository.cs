using System.Data;

namespace DbNetTimeCore.Repositories
{
    public class DbNetTimeRepository : DbRepository, IDbNetTimeRepository
    {
        public DbNetTimeRepository(IConfiguration configuration, IWebHostEnvironment env) : base(configuration, env)
        {
        }
        public DataTable GetCustomers()
        {
            return GetDataTable("select * from customer");
        }
    }
}
