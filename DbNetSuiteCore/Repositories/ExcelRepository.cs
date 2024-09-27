using DbNetSuiteCore.Models;
using System.Data;
using DbNetSuiteCore.Extensions;
using System.Data.OleDb;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;

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
        public async Task GetRecords(GridModel gridModel)
        {
            var dataTable = await BuildDataTable(gridModel);
            dataTable.FilterAndSort(gridModel);
            gridModel.ConvertEnumLookups();
        }

        public async Task GetRecord(GridModel gridModel)
        {
            var dataTable = await BuildDataTable(gridModel);
            dataTable.FilterWithPrimaryKey(gridModel);
            gridModel.ConvertEnumLookups();
        }

        public async Task<DataTable> GetColumns(GridModel gridModel)
        {
            return await BuildDataTable(gridModel);
        }

        private async Task<DataTable> BuildDataTable(GridModel gridModel)
        {
            try
            {
                using (OleDbConnection connection = GetConnection(gridModel.Url))
                {
                    connection.Open();

                    if (string.IsNullOrEmpty(gridModel.TableName))
                    {
                        AssignTableName(gridModel, connection);
                    }

                    DataTable dataTable = new DataTable();
                    OleDbCommand command = new OleDbCommand($"SELECT * FROM [{gridModel.TableName}]", connection);
                    dataTable.Load(await command.ExecuteReaderAsync());
                    connection.Close();
                    return dataTable;
                }
            }
            catch (Exception ex)
            {
                if (gridModel.Url.ToLower().EndsWith(".csv"))
                {
                    return CsvToDataTable(gridModel);
                }
                else
                { 
                    throw new Exception($"Unable too read the Excel file {gridModel.TableName}");
                }
            }
        }

        private DataTable CsvToDataTable(GridModel gridModel)
        {
            DataTable dt = new DataTable();
            List<string> badData = new List<string>();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                BadDataFound = null
            };
            using (var reader = new StreamReader(FilePath(gridModel.Url)))
            using (var csv = new CsvReader(reader, config))
            {
                using (var dr = new CsvDataReader(csv))
                {
                    dt.Load(dr);
                }
            }

            badData.Clear();

            return dt;
        }

        private async Task AssignTableName(GridModel gridModel, OleDbConnection connection)
        {
            if (gridModel.Url.ToLower().EndsWith(".csv"))
            {
                gridModel.TableName = gridModel.Url.Split("/").Last();
            }
            else
            {
                DataTable tables = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                gridModel.TableName = tables.Rows[0]["TABLE_NAME"].ToString();
            }
        }

        public OleDbConnection GetConnection(string filePath)
        {
            return new OleDbConnection(ExcelConnectionString(filePath));
        }

        private string FilePath(string filePath)
        {
            return $"{_env.WebRootPath}{filePath.Replace("/", "\\")}".Replace("//", "/");
        }

        private string ExcelConnectionString(string filePath)
        {
            string properties = filePath.ToLower().EndsWith(".csv") ? "Text;HDR=Yes;FORMAT=Delimited" : "Excel 12.0;HDR=YES";

            if (filePath.ToLower().EndsWith(".csv"))
            {
                filePath = string.Join("/", filePath.Split("/").Reverse().Skip(1).Reverse().ToArray());
            }
            return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={FilePath(filePath)};Extended Properties=\"{properties}\";";
        }
    }
}
