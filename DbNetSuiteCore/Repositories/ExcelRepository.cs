using DbNetSuiteCore.Models;
using System.Data;
using DbNetSuiteCore.Extensions;
using System.Data.OleDb;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;
using Microsoft.Extensions.Caching.Memory;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.Repositories
{
    public class ExcelRepository : IExcelRepository
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _memoryCache;

        public ExcelRepository(IConfiguration configuration, IWebHostEnvironment env, IMemoryCache memoryCache)
        {
            _configuration = configuration;
            _env = env;
            _memoryCache = memoryCache;
        }
        public async Task GetRecords(GridModel gridModel)
        {
            QueryCommandConfig query = gridModel.BuildQuery();
            gridModel.Data = await BuildDataTable(gridModel, query);
            gridModel.ConvertEnumLookups();
            gridModel.GetDistinctLookups();
        }

        public async Task GetRecord(GridModel gridModel)
        {
            QueryCommandConfig query = gridModel.BuildRecordQuery();
            gridModel.Data = await BuildDataTable(gridModel, query);
            gridModel.ConvertEnumLookups();
        }

        public async Task<DataTable> GetColumns(GridModel gridModel)
        {
            if (string.IsNullOrEmpty(gridModel.TableName))
            {
                AssignTableName(gridModel);
            }

            gridModel.TableName = TextHelper.DelimitColumn(gridModel.TableName, DataSourceType.Excel);

            QueryCommandConfig query = gridModel.BuildEmptyQuery();

            return await BuildDataTable(gridModel, query);
        }

        private async Task<DataTable> BuildDataTable(GridModel gridModel, QueryCommandConfig query)
        {
            return await LoadSpreadsheet(gridModel, query);
        }

        private async Task<DataTable> LoadSpreadsheet(GridModel gridModel, QueryCommandConfig query)
        {
            DataTable dataTable;
            try
            {
                using (OleDbConnection connection = GetConnection(gridModel.Url))
                {
                    connection.Open();

                    dataTable = new DataTable();

                    string columns = gridModel.Columns.Any() ? String.Join(",", gridModel.Columns.Select(c => c.Expression)) : "*";

                    OleDbCommand command = new OleDbCommand(query.Sql, connection);
                    DbHelper.AddCommandParameters(command, query.Params);
                    dataTable.Load(await command.ExecuteReaderAsync());
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                /*
            if (gridModel.Url.ToLower().EndsWith(".csv"))
            {
                dataTable = CsvToDataTable(gridModel);
            }
            else
            {
                throw new Exception($"Unable too read the Excel file {gridModel.TableName}");
            }
            */
                throw new Exception($"Unable to read the Excel file {gridModel.TableName}", ex);
            }

            return dataTable;
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

        private async Task AssignTableName(GridModel gridModel)
        {
            if (gridModel.Url.ToLower().EndsWith(".csv"))
            {
                gridModel.TableName = FilePath(gridModel.Url).Split("\\").Last();
            }
            else
            {
                using (OleDbConnection connection = GetConnection(gridModel.Url))
                {
                    connection.Open();
                    DataTable tables = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                    gridModel.TableName = tables.Rows[0]["TABLE_NAME"].ToString();
                    connection.Close();
                }
            }
        }

        public OleDbConnection GetConnection(string filePath)
        {
            return new OleDbConnection(ExcelConnectionString(filePath));
        }

        private string FilePath(string filePath)
        {
            if (TextHelper.IsAbsolutePath(filePath))
            {
                return filePath;

            }
            return $"{_env.WebRootPath}{filePath.Replace("/", "\\")}".Replace("//", "/");
        }

        private string ExcelConnectionString(string filePath)
        {
            string properties = filePath.ToLower().EndsWith(".csv") ? "Text;FORMAT=Delimited;" : "Excel 12.0;";
            properties = $"{properties}HDR=Yes;characterset=65001";

            filePath = FilePath(filePath);

            if (filePath.ToLower().EndsWith(".csv"))
            {
                string separator = "\\";
                filePath = string.Join(separator, filePath.Split(separator).Reverse().Skip(1).Reverse().ToArray());
            }
            return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={filePath};Extended Properties=\"{properties}\";";
        }
    }
}