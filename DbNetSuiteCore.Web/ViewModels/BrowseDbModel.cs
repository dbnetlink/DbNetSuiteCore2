using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;

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

        public Type ControlType = typeof(GridModel);

        private IConfiguration configuration;
        private IWebHostEnvironment? env;
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
                Databases = DbHelper.GetDatabases(ConnectionAlias, configuration);
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
                Tables = DbHelper.GetTables(ConnectionAlias, configuration, DatabaseName);
            }
            else
            {
                Tables = DbHelper.GetTables(ConnectionAlias, DataSourceType, configuration, env);
            }

            if (string.IsNullOrEmpty(TableName) == false && TableName != "All")
            {
                if (Tables.Any(t => t == TableName) == false)
                {
                    TableName = string.Empty;
                }
            }
        }
    }
}