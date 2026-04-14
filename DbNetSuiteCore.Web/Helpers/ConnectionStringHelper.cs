using DbNetSuiteCore.Enums;
using Microsoft.Extensions.Primitives;

namespace DbNetSuiteCore.Web.Helpers
{
    public static class ConnectionStringHelper
    {
        private static Dictionary<DataSourceType, string> ConnectionString = new Dictionary<DataSourceType, string>()
        { 
            {DataSourceType.MSSQL, "Server=localhost;Database={0};Trusted_Connection=True;TrustServerCertificate=True;"},
            {DataSourceType.MySql, "server=localhost;database={0};user=root;password=password1234;"},
            {DataSourceType.PostgreSql, "Host=localhost;Username=postgres;Password=password1234;Database={0};pooling=false;"},
            {DataSourceType.MongoDB, "{0}"}, 
            {DataSourceType.SQLite, "Data Source=~/data/sqlite/{0}.db;Cache=Shared;"}
        };

        private static Dictionary<DataSourceType, string> ConnectionAlias = new Dictionary<DataSourceType, string>()
        {
            {DataSourceType.MSSQL, "Northwind(mssql)"},
            {DataSourceType.MySql, "Northwind2(mysql)"},
            {DataSourceType.PostgreSql, "Northwind(postgresql)"},
            {DataSourceType.MongoDB, "Northwind"},
            {DataSourceType.SQLite, "Northwind(sqlite)"}
        };
        public static string TestConnectionString(StringValues db, DataSourceType dataSourceType)
        {
           return db == StringValues.Empty ? ConnectionAlias[dataSourceType] : string.Format(ConnectionString[dataSourceType], db.ToString());
        }
    }
}