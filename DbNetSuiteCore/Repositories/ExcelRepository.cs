using DbNetSuiteCore.Models;
using System.Data;
using DbNetSuiteCore.Extensions;
using System.Data.OleDb;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.Repositories
{
    public class ExcelRepository : IExcelRepository
    {
        private readonly IWebHostEnvironment _env;

        public ExcelRepository(IWebHostEnvironment env)
        {
            _env = env;
        }
        public async Task GetRecords(ComponentModel componentModel)
        {
            QueryCommandConfig query = componentModel.BuildQuery();
            componentModel.Data = await BuildDataTable(componentModel, query);
            if (componentModel is GridModel)
            {
                var gridModel = (GridModel)componentModel;
                gridModel.ConvertEnumLookups();
                gridModel.GetDistinctLookups();
            }
        }

        public async Task GetRecord(ComponentModel componentModel)
        {
            QueryCommandConfig query = componentModel.BuildRecordQuery();
            componentModel.Data = await BuildDataTable(componentModel, query);
            componentModel.ConvertEnumLookups();
        }

        public async Task<DataTable> GetColumns(ComponentModel componentModel)
        {
            if (string.IsNullOrEmpty(componentModel.TableName))
            {
                AssignTableName(componentModel);
            }

            componentModel.TableName = TextHelper.DelimitColumn(componentModel.TableName, DataSourceType.Excel);

            QueryCommandConfig query = componentModel.BuildEmptyQuery();

            return await BuildDataTable(componentModel, query);
        }


        private async Task<DataTable> BuildDataTable(ComponentModel componentModel, QueryCommandConfig query)
        {
            return await LoadSpreadsheet(componentModel, query);
        }

        private async Task<DataTable> LoadSpreadsheet(ComponentModel componentModel, QueryCommandConfig query)
        {
            DataTable dataTable;
            try
            {
                using (OleDbConnection connection = GetConnection(componentModel.Url))
                {
                    connection.Open();

                    dataTable = new DataTable();

                    string columns = componentModel.GetColumns().Any() ? String.Join(",", componentModel.GetColumns().Select(c => c.Expression)) : "*";

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
                throw new Exception($"Unable to read the Excel file {componentModel.TableName}", ex);
            }

            return dataTable;
        }

        private DataTable CsvToDataTable(ComponentModel componentModel)
        {
            DataTable dt = new DataTable();
            List<string> badData = new List<string>();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                BadDataFound = null
            };
            using (var reader = new StreamReader(FilePath(componentModel.Url)))
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

        private async Task AssignTableName(ComponentModel componentModel)
        {
            if (componentModel.Url.ToLower().EndsWith(".csv"))
            {
                componentModel.TableName = FilePath(componentModel.Url).Split("\\").Last();
            }
            else
            {
                using (OleDbConnection connection = GetConnection(componentModel.Url))
                {
                    connection.Open();
                    DataTable tables = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                    componentModel.TableName = tables.Rows[0]["TABLE_NAME"].ToString();
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