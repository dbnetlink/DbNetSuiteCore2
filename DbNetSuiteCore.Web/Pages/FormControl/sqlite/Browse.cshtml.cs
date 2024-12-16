using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Mvc;
using DbNetSuiteCore.Web.ViewModels;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.Web.Pages.sqlite
{
    [IgnoreAntiforgeryToken]
    public class SqLiteBrowseFormnModel : BrowseDbModel
    {
        public SqLiteBrowseFormnModel(IConfiguration configuration, IWebHostEnvironment env) : base(configuration, env)
        {
            DataSourceType = DataSourceType.SQLite;
            ControlType = typeof(FormModel);
            Connections = GetSQLiteDatabases();
        }
    }
}