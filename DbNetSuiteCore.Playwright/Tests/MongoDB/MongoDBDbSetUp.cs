using DbNetSuiteCore.Web.Helpers;
using MongoDB.Driver;
using NUnit.Framework;
using MongoDB.Bson;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DbNetSuiteCore.Playwright.Tests.MongoDB
{
    public class MongoDBDbSetUp : DbSetUp
    {
        public MongoDBDbSetUp()
        {
            MasterConnectionString = "mongodb://localhost:27017/";
            ConnectionString = ConnectionStringHelper.TestConnectionString(DatabaseName, Enums.DataSourceType.MongoDB);
        }

        [OneTimeSetUp]
        public void DbOneTimeSetUp()
        {

            CreateDatabase();
        }

        [OneTimeTearDown]
        public void DbOneTimeTearDown()
        {
            var client = new MongoClient(MasterConnectionString);
            client.DropDatabase(DatabaseName);
        }

        private void CreateDatabase()
        {
            var client = new MongoClient(MasterConnectionString);
            var database = client.GetDatabase(DatabaseName);

            var resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();

            foreach (var resourceName in resourceNames)
            {
                if (resourceName.Contains("TestDatabase.MongoDB"))
                {
                    var json = LoadTextFromResource(resourceName);
                    var collectionName = Regex.Replace(resourceName, ".json$", string.Empty).Split(".").Last();
                    var collection = database.GetCollection<BsonDocument>(collectionName);
                    var importer = new DynamicDataImporter(database);
                    importer.ImportJsonToMongoDB(json, collectionName);
                }
            }
        }
    }
}