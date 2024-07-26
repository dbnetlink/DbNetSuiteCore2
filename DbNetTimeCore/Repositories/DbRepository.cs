using DbNetTimeCore.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.Collections.Specialized;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text.RegularExpressions;
using static DbNetTimeCore.Utilities.DbNetDataCore;

namespace DbNetTimeCore.Repositories
{
    public class DbRepository
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private Assembly _providerAssembly;
        private IDbDataAdapter _adapter;
        private IDbConnection _connection;
        private DbCommand _command;
        private IDataReader _reader;
        private IDbTransaction _transaction;
        private DataProvider _dataProvider = DataProvider.SqlClient;


        public DbRepository(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        public void Open(string database)
        {
            string connectionString = _configuration.GetConnectionString(database);
            connectionString = MapDatabasePath(connectionString);

            _dataProvider = Regex.IsMatch(connectionString, @"Data Source=(.*)\.db;", RegexOptions.IgnoreCase) ? DataProvider.SQLite : DataProvider.SqlClient;

            if (_dataProvider == DataProvider.SQLite)
            {
                _connection = new SqliteConnection(connectionString);
                _providerAssembly = Assembly.GetAssembly(typeof(SqliteConnection));
            }
            else
            {
                _connection = new SqlConnection(connectionString);
                _providerAssembly = Assembly.GetAssembly(typeof(SqlConnection));
            }

            _command = (DbCommand)_connection.CreateCommand();
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
        }
        public async Task<DataTable> GetDataTable(QueryCommandConfig queryCommandConfig, string database)
        {
            DataTable dataTable = new DataTable();
            dataTable.Load(await ExecuteQuery(queryCommandConfig, database));
            return dataTable;
        }

        public async Task<DbDataReader> ExecuteQuery(string sql, string database)
        {
            return await ExecuteQuery(new QueryCommandConfig(sql), database);
        }
        public async Task<DbDataReader> ExecuteQuery(QueryCommandConfig query, string database)
        {
            ConfigureCommand(query.Sql, query.Params, database);
            return await _command.ExecuteReaderAsync(CommandBehavior.Default);
        }

        public async Task<int> ExecuteNonQuery(CommandConfig commandConfig, string database)
        {
            if (Regex.Match(commandConfig.Sql, "^(delete|update) ", RegexOptions.IgnoreCase).Success)
                if (!Regex.Match(commandConfig.Sql, " where ", RegexOptions.IgnoreCase).Success)
                    throw new Exception("Unqualified updates and deletes are not allowed.");

            ConfigureCommand(commandConfig.Sql, commandConfig.Params, database);
            int returnValue = 0;

            try
            {
                returnValue = await _command.ExecuteNonQueryAsync();
            }
            catch (Exception Ex)
            {
            }

            return returnValue;
        }

        private string MapDatabasePath(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString) || connectionString.EndsWith(".json"))
                return connectionString;

            if (!connectionString.EndsWith(";"))
                connectionString += ";";

            string dataDirectory = String.Empty;

            if (AppDomain.CurrentDomain.GetData("DataDirectory") != null)
                dataDirectory = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();

            if (connectionString.Contains("|DataDirectory|") && dataDirectory != String.Empty)
                connectionString = connectionString.Replace("|DataDirectory|", dataDirectory);

            connectionString = Regex.Replace(connectionString, @"DataProvider=(.*?);", "", RegexOptions.IgnoreCase);

            string currentPath = "";

            if (_env != null)
                currentPath = _env.WebRootPath;

            string dataSourcePropertyName = "data source";

            connectionString = Regex.Replace(connectionString, dataSourcePropertyName + "=~", dataSourcePropertyName + "=" + currentPath, RegexOptions.IgnoreCase).Replace("=//", "=/");
            return connectionString;
        }

        private void ConfigureCommand(string sql, Dictionary<string,object> @params, string database)
        {
            CloseReader();
            Open(database);
            _command.CommandText = sql.Trim();
            _command.CommandType = CommandType.Text;

            _command.Parameters.Clear();
            AddCommandParameters(@params);
        }

        private void AddCommandParameters(Dictionary<string, object> @params)
        {
            if (@params == null)
                return;

            foreach (string key in @params.Keys)
            {
                IDbDataParameter dbParam;

                if (@params[key] is IDbDataParameter)
                {
                    dbParam = @params[key] as IDbDataParameter;
                }
                else
                {
                    dbParam = _command.CreateParameter();
                    dbParam.ParameterName = ParameterName(key);
                    dbParam.Value = @params[key];
                }

                if (dbParam.Value == null)
                {
                    dbParam.Value = DBNull.Value;
                }

                _command.Parameters.Add(dbParam);
            }
        }

        private void CloseReader()
        {
            if (_reader is IDataReader)
            {
                if (!_reader.IsClosed)
                {
                    try
                    {
                        _command.Cancel();
                    }
                    catch (Exception)
                    {
                    }

                    _reader.Close();
                }
            }
        }

        private string ParameterName(string key, bool parameterValue = false)
        {
            var temmplate = "@{0}";
            if (key.Length > 0)
                if (temmplate.Substring(0, 1) == key.Substring(0, 1))
                    return key;

            return temmplate.Replace("{0}", CleanParameterName(key));
        }

        private string CleanParameterName(string key)
        {
            key = Regex.Replace(key, "[^a-zA-Z0-9_]", "_");
            return key;
        }
    }
}
