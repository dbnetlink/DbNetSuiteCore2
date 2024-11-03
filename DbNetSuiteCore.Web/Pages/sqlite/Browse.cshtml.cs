using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Mvc;
using DbNetSuiteCore.Web.ViewModels;

namespace DbNetSuiteCore.Web.Pages.sqlite
{
    [IgnoreAntiforgeryToken]
    public class SqLiteBrowseModel : BrowseDbModel
    {
        public SqLiteBrowseModel(IConfiguration configuration, IWebHostEnvironment env) : base(configuration, env)
        {
            DataSourceType = DataSourceType.SQLite;
            Connections = new List<string> { "Sakila(sqlite)", "Chinook(sqlite)", "Northwind(sqlite)", "Euro(sqlite)" };
        }
    }
}