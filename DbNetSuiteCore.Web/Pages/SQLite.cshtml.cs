using DbNetSuiteCore.Enums;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Text.RegularExpressions;

namespace DbNetSuiteCore.Web.Pages
{
    public class SQLiteModel : PageModel
    {
        public DataTable Tables { get; set; } = new DataTable();
        public List<string> Connections { get; set; } = new List<string>() { "Sakila","Chinook"};

        [BindProperty]
        public string TableName { get; set; } = string.Empty;
        [BindProperty]
        public string ConnectionAlias { get; set; } = string.Empty;

        private IConfiguration configuration;
        private IWebHostEnvironment env;
        public SQLiteModel(IConfiguration configuration, IWebHostEnvironment env)
        {
            this.configuration = configuration;
            this.env = env;
        }
        public void OnGet()
        {
            LoadTables();
        }

        public void OnPost()
        {
            LoadTables();
        }

        private void LoadTables()
        {
            if (string.IsNullOrEmpty(ConnectionAlias))
            {
                return;
            }
            var connection = GetConnection();
            connection.Open();
  
            var command = ConfigureCommand("SELECT name FROM sqlite_master WHERE type = 'table' order by 1", connection);
            Tables.Load(command.ExecuteReader(CommandBehavior.Default));
            connection.Close();
        }

        private IDbCommand ConfigureCommand(string sql, IDbConnection connection)
        {
            IDbCommand command = connection.CreateCommand();
            command.CommandText = sql.Trim();
            command.CommandType = CommandType.Text;
            command.Parameters.Clear();
            command.CommandText = sql.Trim();
            return command;
        }

        private SqliteConnection GetConnection()
        {
            string? connectionString = this.configuration.GetConnectionString(ConnectionAlias);
            connectionString = MapDatabasePath(connectionString);
            SqliteConnection connection = new SqliteConnection(connectionString);
            return connection;
        }

        private string MapDatabasePath(string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return string.Empty;

            if (!connectionString.EndsWith(";"))
                connectionString += ";";

            string dataDirectory = String.Empty;

            if (AppDomain.CurrentDomain.GetData("DataDirectory") != null)
                dataDirectory = AppDomain.CurrentDomain.GetData("DataDirectory")?.ToString() ?? string.Empty;

            if (connectionString.Contains("|DataDirectory|") && dataDirectory != String.Empty)
                connectionString = connectionString.Replace("|DataDirectory|", dataDirectory);

            connectionString = Regex.Replace(connectionString, @"DataProvider=(.*?);", "", RegexOptions.IgnoreCase);

            string currentPath = env.WebRootPath; 

            string dataSourcePropertyName = "data source";

            connectionString = Regex.Replace(connectionString, dataSourcePropertyName + "=~", dataSourcePropertyName + "=" + currentPath, RegexOptions.IgnoreCase).Replace("=//", "=/");
            return connectionString;
        }
    }
}
