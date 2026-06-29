using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Mvc;
using DbNetSuiteCore.Web.ViewModels;

namespace DbNetSuiteCore.Web.Pages.DuckDb
{
    [IgnoreAntiforgeryToken]
    public class DuckDbBrowseModel : BrowseDbModel
    {
        public DuckDbBrowseModel(IConfiguration configuration, IWebHostEnvironment env) : base(configuration, env)
        {
            DataSourceType = DataSourceType.DuckDB;
            Connections = GetDuckDbDatabases();
        }
    }
}