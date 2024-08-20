using DbNetSuiteCore.Enums;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DbNetSuiteCore.Web.Pages
{
    public class AWModel : PageModel
    {
        public DataTable Tables { get; set; } = new DataTable();
        public List<string> Connections { get; set; } = new List<string>() { "AdventureWorks","Northwind"};

        [BindProperty]
        public string TableName { get; set; } = string.Empty;
        [BindProperty]
        public string ConnectionAlias { get; set; } = string.Empty;

        private IConfiguration configuration;
        public AWModel(IConfiguration configuration)
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
            connection.Close();
        }

        private SqlConnection GetConnection()
        {
            string? connectionString = this.configuration.GetConnectionString(ConnectionAlias);
            SqlConnection connection = new SqlConnection(connectionString);
            return connection;
        }
    }
}
