using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Mvc;
using DbNetSuiteCore.Web.ViewModels;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.Web.Pages.Oracle
{
    [IgnoreAntiforgeryToken(Order = 2000)]
    public class OracleBrowseFormDbModel : BrowseDbModel
    {
        public OracleBrowseFormDbModel(IConfiguration configuration) : base(configuration)
        {
            DataSourceType = DataSourceType.Oracle;
            ControlType = typeof(FormModel);
            Connections = DbHelper.GetConnections(configuration).Where(c => c.Value.ToLower().Contains("localhost:1521")).Select(c => c.Key).ToList();
        }
    }
}