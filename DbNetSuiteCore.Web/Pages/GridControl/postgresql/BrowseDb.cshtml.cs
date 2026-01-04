using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Mvc;
using DbNetSuiteCore.Web.ViewModels;
using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.Web.Pages.mssql
{
    [IgnoreAntiforgeryToken]
    public class PostgreSqlBrowseDbModel : BrowseDbModel
    {
        public PostgreSqlBrowseDbModel(IConfiguration configuration) : base(configuration)
        {
            DataSourceType = DataSourceType.PostgreSql;
            Connections = Connections = DbHelper.GetConnections(configuration).Where(c => c.Value.ToLower().Contains("port=5432")).Select(c => c.Key).ToList();
        }
    }
}