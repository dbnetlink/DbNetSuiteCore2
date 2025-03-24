using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Mvc;
using DbNetSuiteCore.Web.ViewModels;
using DbNetSuiteCore.Helpers;
using Oracle.ManagedDataAccess.Client;

namespace DbNetSuiteCore.Web.Pages.Oracle
{
    [IgnoreAntiforgeryToken(Order = 2000)]
    public class OracleBrowseDbModel : BrowseDbModel
    {
        public OracleBrowseDbModel(IConfiguration configuration) : base(configuration)
        {
            OracleDbType OracleDbType = OracleDbType.BFile;


            DataSourceType = DataSourceType.Oracle;
            Connections = DbHelper.GetConnections(configuration).Where(c => c.Value.ToLower().Contains("localhost:1521")).Select(c => c.Key).ToList();
        }
    }
}