using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Text.RegularExpressions;
using DbNetSuiteCore.Repositories;
using DocumentFormat.OpenXml.Spreadsheet;

namespace DbNetSuiteCore.Web.ViewModels
{
    public class BrowseDbModel : PageModel
    {
        public DataSourceType DataSourceType { get; set; }
        public List<string> Tables { get; set; } = new List<string>();
        public List<string> Connections { get; set; } = new List<string>();

        [BindProperty]
        public string TableName { get; set; } = string.Empty;
        [BindProperty]
        public string ConnectionAlias { get; set; } = string.Empty;

        private IConfiguration configuration;
        private IWebHostEnvironment? env;
        public BrowseDbModel(IConfiguration configuration, IWebHostEnvironment? env = null)
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

        public void LoadTables()
        {
            if (string.IsNullOrEmpty(ConnectionAlias))
            {
                return;
            }
            var connection = GetConnection();
            connection.Open();

            switch(DataSourceType)
            {
                case DataSourceType.MSSQL:
                    LoadMSSQLTables(connection as SqlConnection);
                    break;
                case DataSourceType.SQlite:
                    LoadSqliteTables(connection as SqliteConnection);
                    break;
                case DataSourceType.PostgreSql:
                    LoadPostreSqlTables(connection);
                    break;
                case DataSourceType.MySql:
                    LoadMySqlTables(connection);
                    break;
            }

            if (string.IsNullOrEmpty(TableName) == false && TableName != "All")
            {
                if (Tables.Any(t => t == TableName) == false)
                {
                    TableName = string.Empty;
                }
            }
            connection.Close();
        }


        private void LoadMSSQLTables(SqlConnection connection)
        {
            var schemaTable = connection.GetSchema("Tables").Select(string.Empty, "TABLE_SCHEMA,TABLE_NAME").CopyToDataTable();

            /*
            if (string.IsNullOrEmpty(TableName) == false && TableName != "All")
            {
                var tableSchema = TableName.Split(".").First();
                var tableName = TableName.Split(".").Skip(1).First().Replace("[", string.Empty).Replace("]", string.Empty);
                if (schemaTable.Select($"TABLE_SCHEMA = '{tableSchema}' and TABLE_NAME = '{tableName}'").Length == 0)
                {
                    TableName = string.Empty;
                }
            }
            */

            foreach (DataRow dataRow in schemaTable.Rows)
            {
                Tables.Add($"{dataRow[1]}.[{dataRow[2]}]");
            }
        }
        private void LoadSqliteTables(SqliteConnection connection)
        {
            LoadSchemaTables("SELECT name FROM sqlite_master WHERE type = 'table' order by 1", connection);
        }

        private void LoadPostreSqlTables(IDbConnection connection)
        {
            LoadSchemaTables("SELECT table_schema || '.' || table_name AS name  FROM information_schema.tables where table_schema = 'public' order by 1", connection);
        }

        private void LoadMySqlTables(IDbConnection connection)
        {
            LoadSchemaTables("SELECT CONCAT(`table_schema`,'.',`table_name`) AS name  FROM information_schema.tables where table_schema in ('northwind','sakila') order by 1", connection);
        }

        private void LoadSchemaTables(string sql, IDbConnection connection)
        {
            var command = DbRepository.ConfigureCommand(sql, connection);

            DataTable schemaTable = new DataTable();
            schemaTable.Load(command.ExecuteReader(CommandBehavior.Default));

            foreach (DataRow dataRow in schemaTable.Rows)
            {
                Tables.Add($"{dataRow[0]}");
            }
        }

        private IDbConnection GetConnection()
        {
            string? connectionString = configuration.GetConnectionString(ConnectionAlias);
            switch (DataSourceType)
            {
                default:
                    return new SqlConnection(connectionString);
                case DataSourceType.SQlite:
                    connectionString = DbRepository.MapDatabasePath(connectionString, env);
                    return new SqliteConnection(connectionString);
                case DataSourceType.PostgreSql:
                case DataSourceType.MySql:
                    return DbRepository.GetCustomDbConnection(DataSourceType, connectionString);
            }
        }
    }
}
