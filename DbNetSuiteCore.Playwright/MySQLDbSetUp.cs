using DbNetSuiteCore.Web.Constants;
using MySqlConnector;
using NUnit.Framework;

namespace DbNetSuiteCore.Playwright
{
    public class MySQLDbSetUp : DbSetUp
    {
       
        public MySQLDbSetUp() 
        {
            MasterConnectionString = "server=localhost;user=root;password=password1234;";
            ConnectionString = string.Format(ConnectionStringTemplates.MySql, DatabaseName);
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
            using (var connection = new MySqlConnection(MasterConnectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = $"DROP DATABASE IF EXISTS `{DatabaseName}`";
                command.ExecuteNonQuery();
            }
        }

        private void CreateDatabase()
        {
            using (var connection = new MySqlConnection(MasterConnectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = $"CREATE DATABASE `{DatabaseName}`";
                command.ExecuteNonQuery();
            }
        }

        private void ExecuteScriptFile()
        {
            var script = LoadScriptFromFile("TestDatabase/MySql/Northwind.sql");

            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = script;
                command.ExecuteNonQuery();
            }
        }
    }
}