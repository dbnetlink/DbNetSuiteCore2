using DuckDB.NET.Data;
using NUnit.Framework;

namespace DbNetSuiteCore.Playwright.Tests.DuckDB
{
    public class DuckDbSetUp : DbSetUp
    {
        public DuckDbSetUp()
        {
            //  ConnectionString = ConnectionStringHelper.TestConnectionString(DatabaseName, DataSourceType.DuckDB);
        }

        [OneTimeSetUp]
        public void DbOneTimeSetUp()
        {
            try
            {
                CreateDb("chinook");
                CreateDb("sakila");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private void CreateDb(string databaseName)
        {
            using (var conn = new DuckDBConnection($"Data Source={databaseName}.duckdb"))
            {
                conn.Open();

                using var cmd = conn.CreateCommand();

                foreach (var file in Directory.GetFiles($"..\\..\\..\\TestDatabase\\CSV\\{databaseName}", "*.csv"))
                {
                    var tableName = Path.GetFileNameWithoutExtension(file);
                    cmd.CommandText = $"CREATE TABLE IF NOT EXISTS {tableName} AS SELECT * FROM read_csv_auto('{file}')";
                    cmd.ExecuteNonQuery();
                }

                conn.Close();
            }

            var sourceDb = $"{databaseName}.duckdb";
            var destinationDb = $"{SolutionFolder()}\\DbNetSuiteCore.Web\\wwwroot\\data\\duckdb\\{databaseName}.duckdb";

            File.Copy(sourceDb, destinationDb, true);
        }

        [OneTimeTearDown]
        public void DbOneTimeTearDown()
        {
        }
    }
}