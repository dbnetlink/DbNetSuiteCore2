using DbNetSuiteCore.Web.Helpers;
using NUnit.Framework;
using DbNetSuiteCore.Enums;

namespace DbNetSuiteCore.Playwright.Tests.PostgreSql
{
    public class SQLiteDbSetUp : DbSetUp
    {

        public SQLiteDbSetUp()
        {
            ConnectionString = ConnectionStringHelper.TestConnectionString(DatabaseName, DataSourceType.SQLite);
        }

        [OneTimeSetUp]
        public async Task DbOneTimeSetUp()
        {
            var dbFolder = $"{SolutionFolder()}\\DbNetSuiteCore.Web\\wwwroot\\data\\sqlite";
            foreach (string path in Directory.GetFiles(dbFolder))
            {
                string file = path.Split("\\").Last();
                if (file.StartsWith("testdb_"))
                {
                    File.Delete(path);
                }
            }
 
            var sourceDb = $"{ProjectFolder()}\\TestDatabase\\sqlite\\northwind.db";
            var destinationDb = $"{SolutionFolder()}\\DbNetSuiteCore.Web\\wwwroot\\data\\sqlite\\{DatabaseName}.db";

            File.Copy(sourceDb, destinationDb);
        }

        [OneTimeTearDown]
        public void DbOneTimeTearDown()
        {
        }
    }
}