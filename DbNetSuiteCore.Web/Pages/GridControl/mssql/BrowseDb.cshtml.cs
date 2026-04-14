using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Mvc;
using DbNetSuiteCore.Web.ViewModels;
using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.Web.Pages.mssql
{
    [IgnoreAntiforgeryToken(Order = 2000)]
    public class MsSqlBrowseDbModel : BrowseDbModel
    {
        public MsSqlBrowseDbModel(IConfiguration configuration) : base(configuration)
        {
            DataSourceType = DataSourceType.MSSQL;
            Connections = DbHelper.GetConnections(configuration).Where(c => c.Value.ToLower().Contains("trusted_connection=true") || c.Value.ToLower().Contains("trustservercertificate=true") ).Select(c => c.Key).ToList();
        }
    }
}