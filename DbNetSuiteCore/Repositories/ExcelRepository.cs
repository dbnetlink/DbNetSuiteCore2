using DbNetSuiteCore.Models;
using System.Data;
using DbNetSuiteCore.Extensions;
using System.Data.OleDb;

namespace DbNetSuiteCore.Repositories
{
    public class ExcelRepository : IExcelRepository
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private static readonly HttpClient _httpClient = new HttpClient();
        public ExcelRepository(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }
        public async Task GetRecords(GridModel gridModel, HttpContext httpContext)
        {
            var dataTable = await BuildDataTable(gridModel, httpContext);
            dataTable.FilterAndSort(gridModel);
            gridModel.ConvertEnumLookups();
        }

        public async Task GetRecord(GridModel gridModel, HttpContext httpContext)
        {
            var dataTable = await BuildDataTable(gridModel, httpContext);
            dataTable.FilterWithPrimaryKey(gridModel);
            gridModel.ConvertEnumLookups();
        }

        public async Task<DataTable> GetColumns(GridModel gridModel, HttpContext httpContext)
        {
            return await BuildDataTable(gridModel, httpContext);
        }

        private async Task<DataTable> BuildDataTable(GridModel gridModel, HttpContext httpContext)
        {
            var connection = GetConnection(gridModel.Url);
            connection.Open();

            if (string.IsNullOrEmpty(gridModel.TableName))
            {
                AssignTableName(gridModel, connection);
            }

            DataTable dataTable = new DataTable();
            OleDbCommand command = new OleDbCommand($"SELECT * FROM [{gridModel.TableName}]", connection);
            dataTable.Load(command.ExecuteReader());
            connection.Close();
            return dataTable;
        }

        private async Task AssignTableName(GridModel gridModel, OleDbConnection connection)
        {
            DataTable tables = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
            gridModel.TableName = tables.Rows[0]["TABLE_NAME"].ToString();
        }

        

        public OleDbConnection GetConnection(string filePath)
        {
            return new OleDbConnection(ExcelConnectionString(filePath));
        }

        private string ExcelConnectionString(string filePath)
        {
            filePath = $"{_env.WebRootPath}/{filePath}".Replace("//", "/".Replace("/", "\\"));
            return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={filePath};Extended Properties=\"Excel 12.0;HDR=YES\";";
        }
    }
}
