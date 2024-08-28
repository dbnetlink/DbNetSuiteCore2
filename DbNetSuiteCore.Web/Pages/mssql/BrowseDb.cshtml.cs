using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Mvc;
using DbNetSuiteCore.Web.ViewModels;

namespace DbNetSuiteCore.Web.Pages.mssql
{
    [IgnoreAntiforgeryToken(Order = 2000)]
    public class MsSqlBrowseDbModel : BrowseDbModel
    {
        public MsSqlBrowseDbModel(IConfiguration configuration) : base(configuration)
        {
            DataSourceType = DataSourceType.MSSQL;
            Connections = new List<string>{ "AdventureWorks", "Northwind" };
        }
    }
}
