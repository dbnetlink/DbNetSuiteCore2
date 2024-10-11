using DbNetSuiteCore.Web.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.MSSQL
{
    public class MSSQLDbSetUp : DbSetUp
    {
        public MSSQLDbSetUp()
        {
            MasterConnectionString = "Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
            ConnectionString = ConnectionStringHelper.TestConnectionString(DatabaseName, Enums.DataSourceType.MSSQL);
        }

        [OneTimeSetUp]
        public void DbOneTimeSetUp()
        {
            CreateDatabase();
            ExecuteScriptFile();
        }

        [OneTimeTearDown]
        public void DbOneTimeTearDown()
        {
            using (var connection = new SqlConnection(MasterConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = $@"
                    ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{DatabaseName}]";
                command.ExecuteNonQuery();
            }
        }

        private void CreateDatabase()
        {
            using (var connection = new SqlConnection(MasterConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = $"CREATE DATABASE [{DatabaseName}]";
                command.ExecuteNonQuery();
            }
        }

        private void ExecuteScriptFile()
        {
            var script = LoadScriptFromFile("TestDatabase/MSSQL/Northwind.sql");

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var server = new Server(new ServerConnection(connection));
                server.ConnectionContext.ExecuteNonQuery(script);
            }
        }
    }
}