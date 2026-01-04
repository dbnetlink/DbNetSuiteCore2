using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Mvc;
using DbNetSuiteCore.Web.ViewModels;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.Web.Pages.mssql
{
    [IgnoreAntiforgeryToken(Order = 2000)]
    public class MsSqlBrowseFormDbModel : BrowseDbModel
    {
        public MsSqlBrowseFormDbModel(IConfiguration configuration) : base(configuration)
        {
            DataSourceType = DataSourceType.MSSQL;
            ControlType = typeof(FormModel);
            Connections = DbHelper.GetConnections(configuration).Where(c => c.Value.ToLower().Contains("trusted_connection=true")).Select(c => c.Key).ToList();
        }
    }
}