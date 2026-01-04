using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using MongoDB.Driver.Core.Configuration;
using Microsoft.Extensions.FileProviders;

namespace DbNetSuiteCore.Web.ViewModels
{
    public class BrowseDbModel : PageModel
    {
        public DataSourceType DataSourceType { get; set; }
        public List<string> Tables { get; set; } = new List<string>();
        public List<string> Databases { get; set; } = new List<string>();

        public List<string> Connections { get; set; } = new List<string>();

        [BindProperty]
        public string TableName { get; set; } = string.Empty;
        [BindProperty]
        public string DatabaseName { get; set; } = string.Empty;
        [BindProperty]
        public string ConnectionAlias { get; set; } = string.Empty;
        public string ConnectionString => string.IsNullOrEmpty(ConnectionAlias) || DataSourceType != DataSourceType.SQLite ? ConnectionAlias : $"Data Source=~/data/sqlite/{ConnectionAlias};Cache=Shared;";

        public Type ControlType = typeof(GridModel);

        protected IConfiguration configuration;
        protected IWebHostEnvironment? env;
        public BrowseDbModel(IConfiguration configuration, IWebHostEnvironment? env = null)
        {
            this.configuration = configuration;
            this.env = env;
        }
        public void OnGet()
        {
            LoadDatabases();
            LoadTables();
        }

        public void OnPost()
        {
            LoadDatabases();
            LoadTables();
        }

        public void LoadDatabases()
        {
            if (string.IsNullOrEmpty(ConnectionAlias))
            {
                return;
            }

            if (DataSourceType == DataSourceType.MongoDB)
            {
                Databases = DbHelper.GetDatabases(ConnectionString, configuration);
            }
        }

        public void LoadTables()
        {
            if (string.IsNullOrEmpty(ConnectionAlias))
            {
                return;
            }

            if (DataSourceType == DataSourceType.MongoDB)
            {
                if (string.IsNullOrEmpty(DatabaseName))
                {
                    return;
                }
                Tables = DbHelper.GetTables(ConnectionString, configuration, DatabaseName);
            }
            else
            {
                Tables = DbHelper.GetTables(ConnectionString, DataSourceType, configuration, env);
            }

            if (string.IsNullOrEmpty(TableName) == false && TableName != "All")
            {
                if (Tables.Any(t => t == TableName) == false)
                {
                    TableName = string.Empty;
                }
            }
        }

        protected List<string> GetSQLiteDatabases()
        {
            var provider = new PhysicalFileProvider($"{env?.WebRootPath}\\data\\sqlite");
            var contents = provider.GetDirectoryContents(string.Empty);

            var databases = new List<string>();
            foreach (IFileInfo file in contents)
            {
                databases.Add(file.Name);
            };
            return databases;
        }
    }
}