using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Mvc;
using DbNetSuiteCore.Web.ViewModels;
using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.Web.Pages.mssql
{
    [IgnoreAntiforgeryToken]
    public class MySqlBrowseDbModel : BrowseDbModel
    {
        public MySqlBrowseDbModel(IConfiguration configuration) : base(configuration)
        {
            DataSourceType = DataSourceType.MySql;
            Connections = DbHelper.GetConnections(configuration).Where(c => c.Value.ToLower().Contains("port=3306")).Select(c => c.Key).ToList();
        }
    }
}