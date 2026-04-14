using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Mvc;
using DbNetSuiteCore.Web.ViewModels;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.Web.Pages.sqlite
{
    [IgnoreAntiforgeryToken]
    public class MongoDbBrowseFormnModel : BrowseDbModel
    {
        public MongoDbBrowseFormnModel(IConfiguration configuration, IWebHostEnvironment env) : base(configuration, env)
        {
            DataSourceType = DataSourceType.MongoDB;
            ControlType = typeof(FormModel);
            Connections = DbHelper.GetConnections(configuration).Where(c => c.Value.ToLower().StartsWith("mongodb")).Select(c => c.Key).ToList();
        }
    }
}