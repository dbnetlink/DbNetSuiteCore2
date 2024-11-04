using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using Microsoft.AspNetCore.Mvc;
using DbNetSuiteCore.Web.ViewModels;

namespace DbNetSuiteCore.Web.Pages.mssql
{
    [IgnoreAntiforgeryToken]
    public class MongoDBBrowseDbModel : BrowseDbModel
    {
        public MongoDBBrowseDbModel(IConfiguration configuration) : base(configuration)
        {
            DataSourceType = DataSourceType.MongoDB;
            Connections = DbHelper.GetConnections(configuration).Where(c => c.Value.ToLower().StartsWith("mongodb")).Select(c => c.Key).ToList();
        }
    }
}