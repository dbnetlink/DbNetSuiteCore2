using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public class SQLiteRepository : DbRepository, ISQLiteRepository
    {
        public SQLiteRepository(IConfiguration configuration, IWebHostEnvironment env) : base(Enums.DataSourceType.SQlite, configuration, env)
        {
        }

    }
}
