using DbNetSuiteCore.Web.Constants;
using Npgsql;
using NUnit.Framework;

namespace DbNetSuiteCore.Playwright
{
    public class PostgreSqlDbSetUp : DbSetUp
    {
        
        public PostgreSqlDbSetUp () 
        {
            MasterConnectionString = "Host=localhost;Username=postgres;Password=password1234;Database=postgres;pooling=false;";
            ConnectionString = string.Format(ConnectionStringTemplates.PostgreSql, DatabaseName);
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
            using (var connection = new NpgsqlConnection(MasterConnectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = $"DROP DATABASE {DatabaseName} WITH (FORCE);";
                command.ExecuteNonQuery();
            }
        }

        private void CreateDatabase()
        {
            using (var connection = new NpgsqlConnection(MasterConnectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = $"CREATE DATABASE {DatabaseName};GRANT ALL PRIVILEGES ON DATABASE {DatabaseName} to postgres";
                command.ExecuteNonQuery();
            }

        }

        private void ExecuteScriptFile()
        {
            var script = LoadScriptFromFile("TestDatabase/PostgreSql/Northwind.sql");

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = script;
                command.ExecuteNonQuery();
            }
        }
    }
}