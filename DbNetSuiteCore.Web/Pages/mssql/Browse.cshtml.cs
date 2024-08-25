using DbNetSuiteCore.Enums;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DbNetSuiteCore.Web.Pages.mssql
{
    [IgnoreAntiforgeryToken]
    public class BrowseModel : PageModel
    {
        public DataTable Tables { get; set; } = new DataTable();
        public List<string> Connections { get; set; } = new List<string>() { "AdventureWorks", "Northwind" };

        [BindProperty]
        public string TableName { get; set; } = string.Empty;
        [BindProperty]
        public string ConnectionAlias { get; set; } = string.Empty;

        private IConfiguration configuration;
        public BrowseModel(IConfiguration configuration)
        {
            this.configuration = configuration;
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
            Tables = connection.GetSchema("Tables").Select(string.Empty, "TABLE_SCHEMA,TABLE_NAME").CopyToDataTable();

            if (string.IsNullOrEmpty(TableName) == false)
            {
                var tableSchema = TableName.Split(".").First();
                var tableName = TableName.Split(".").Skip(1).First().Replace("[",string.Empty).Replace("]", string.Empty);
                if (Tables.Select($"TABLE_SCHEMA = '{tableSchema}' and TABLE_NAME = '{tableName}'").Length == 0)
                {
                    TableName = string.Empty;
                }
            }
            connection.Close();
        }

        private SqlConnection GetConnection()
        {
            string? connectionString = configuration.GetConnectionString(ConnectionAlias);
            SqlConnection connection = new SqlConnection(connectionString);
            return connection;
        }
    }
}
