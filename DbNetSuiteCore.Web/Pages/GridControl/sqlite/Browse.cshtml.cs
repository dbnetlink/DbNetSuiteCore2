using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Mvc;
using DbNetSuiteCore.Web.ViewModels;
using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.Web.Pages.sqlite
{
    [IgnoreAntiforgeryToken]
    public class SqLiteBrowseModel : BrowseDbModel
    {
        public SqLiteBrowseModel(IConfiguration configuration, IWebHostEnvironment env) : base(configuration, env)
        {
            DataSourceType = DataSourceType.SQLite;
            Connections = DbHelper.GetConnections(configuration).Where(c => c.Value.ToLower().Contains("cache=shared")).Select(c => c.Key).ToList();
        }
    }
}