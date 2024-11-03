using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Mvc;
using DbNetSuiteCore.Web.ViewModels;

namespace DbNetSuiteCore.Web.Pages.mssql
{
    [IgnoreAntiforgeryToken]
    public class MySqlBrowseDbModel : BrowseDbModel
    {
        public MySqlBrowseDbModel(IConfiguration configuration) : base(configuration)
        {
            DataSourceType = DataSourceType.MySql;
            Connections = new List<string>{ "northwind(mysql)", "northwind2(mysql)" };
        }
    }
}
