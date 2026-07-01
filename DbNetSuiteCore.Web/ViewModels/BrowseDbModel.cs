using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
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
        public string ConnectionString
        {
            get
            {
                switch(DataSourceType)
                {
                    case DataSourceType.SQLite:
                        return $"Data Source=~/data/sqlite/{ConnectionAlias};Cache=Shared;";
                    case DataSourceType.DuckDB:
                        return $"Data Source=~/data/duckdb/{ConnectionAlias};ACCESS_MODE=READ_ONLY;";
                    default:
                        return ConnectionAlias; 
                }
            }
        }

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
        }

        public void LoadTables()
        {
            if (string.IsNullOrEmpty(ConnectionAlias))
            {
                return;
            }
            Tables = DbHelper.GetTables(ConnectionString, DataSourceType, configuration, env);

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
            return GetFileDatabases("sqlite");
        }

        protected List<string> GetDuckDbDatabases()
        {
            return GetFileDatabases("duckdb");
        }

        private List<string> GetFileDatabases(string subfolder)
        {
            var provider = new PhysicalFileProvider($"{env?.WebRootPath}\\data\\{subfolder}");
            var contents = provider.GetDirectoryContents(string.Empty);

            var databases = new List<string>();
            foreach (IFileInfo file in contents)
            {
                databases.Add(file.Name);
            }
            return databases;
        }
    }
}