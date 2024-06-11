using DbNetTimeCore.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.Collections.Specialized;
using System.Data;
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
        private IDbCommand _command;
        private IDataReader _reader;
        private IDbTransaction _transaction;
        private DataProvider _dataProvider;


        public DbRepository(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;

            string connectionString = _configuration.GetConnectionString("dbnettime") ?? string.Empty;
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

            _command = _connection.CreateCommand();
        }

        public void Open()
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
        }
        public DataTable GetDataTable(QueryCommandConfig queryCommandConfig)
        {
            DataTable dataTable = new DataTable();
            dataTable.Load(ExecuteQuery(queryCommandConfig));
            return dataTable;
        }

        public IDataReader ExecuteQuery(string sql)
        {
            return ExecuteQuery(new QueryCommandConfig(sql));
        }
        public IDataReader ExecuteQuery(QueryCommandConfig query)
        {
            ConfigureCommand(query.Sql, query.Params);
            return _command.ExecuteReader(CommandBehavior.Default);
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

        private void ConfigureCommand(string sql, ListDictionary @params)
        {
            CloseReader();
            Open();
            _command.CommandText = sql.Trim();
            _command.CommandType = CommandType.Text;

            _command.Parameters.Clear();
            AddCommandParameters(@params);
        }

        private void AddCommandParameters(ListDictionary @params)
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
