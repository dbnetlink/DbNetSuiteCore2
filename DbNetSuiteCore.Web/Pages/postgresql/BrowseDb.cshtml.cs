using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Mvc;
using DbNetSuiteCore.Web.ViewModels;

namespace DbNetSuiteCore.Web.Pages.mssql
{
    [IgnoreAntiforgeryToken]
    public class PostgreSqlBrowseDbModel : BrowseDbModel
    {
        public PostgreSqlBrowseDbModel(IConfiguration configuration) : base(configuration)
        {
            DataSourceType = DataSourceType.PostgreSql;
            Connections = new List<string>{ "sakila(postgresql)" };
        }
    }
}
