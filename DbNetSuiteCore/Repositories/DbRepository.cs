using DbNetSuiteCore.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace DbNetSuiteCore.Repositories
{
    public class DbRepository : BaseRepository
    {
        private readonly DataProvider _dataProvider;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public DbRepository(DataProvider dataProvider, IConfiguration configuration, IWebHostEnvironment env)
        {
            _dataProvider = dataProvider;
            _configuration = configuration;
            _env = env;
        }

        public IDbConnection GetConnection(string database)
        {
            string? connectionString = _configuration.GetConnectionString(database);
            connectionString = MapDatabasePath(connectionString);

            IDbConnection connection;

            switch (_dataProvider)
            {
                case DataProvider.SQLite:
                    connection = new SqliteConnection(connectionString);
                    break;
                default:
                    connection = new SqlConnection(connectionString);
                    break;
            }

            return connection;
        }

        public async Task<DataTable> GetDataTable(QueryCommandConfig queryCommandConfig, string database)
        {
            var connection = GetConnection(database);
            connection.Open();
            DataTable dataTable = new DataTable();
            dataTable.Load(await ExecuteQuery(queryCommandConfig, connection));
            connection.Close();
            return dataTable;
        }

        public async Task<DataTable> GetSchemaTable(QueryCommandConfig queryCommandConfig, string database)
        {
            var connection = GetConnection(database);
            connection.Open();
            var reader = await ExecuteQuery(queryCommandConfig, connection, CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo);
            DataTable dataTable = reader.GetSchemaTable() ?? new DataTable();
            connection.Close();
            return dataTable;
        }

        public async Task<DbDataReader> ExecuteQuery(string sql, IDbConnection connection)
        {
            return await ExecuteQuery(new QueryCommandConfig(sql), connection);
        }
        public async Task<DbDataReader> ExecuteQuery(QueryCommandConfig query, IDbConnection connection, CommandBehavior Behaviour = CommandBehavior.Default)
        {
            IDbCommand command = ConfigureCommand(query.Sql, query.Params, connection);
            return await ((DbCommand)command).ExecuteReaderAsync(CommandBehavior.Default);
        }

        public async Task<int> ExecuteNonQuery(CommandConfig commandConfig, IDbConnection connection)
        {
            if (Regex.Match(commandConfig.Sql, "^(delete|update) ", RegexOptions.IgnoreCase).Success)
                if (!Regex.Match(commandConfig.Sql, " where ", RegexOptions.IgnoreCase).Success)
                    throw new Exception("Unqualified updates and deletes are not allowed.");

            IDbCommand command = ConfigureCommand(commandConfig.Sql, commandConfig.Params, connection);
            int returnValue = 0;

            try
            {
                returnValue = await ((DbCommand)command).ExecuteNonQueryAsync();
            }
            catch (Exception)
            {
            }

            return returnValue;
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

            string currentPath = "";

            if (_env != null)
                currentPath = _env.WebRootPath;

            string dataSourcePropertyName = "data source";

            connectionString = Regex.Replace(connectionString, dataSourcePropertyName + "=~", dataSourcePropertyName + "=" + currentPath, RegexOptions.IgnoreCase).Replace("=//", "=/");
            return connectionString;
        }

        private IDbCommand ConfigureCommand(string sql, Dictionary<string,object> @params, IDbConnection connection)
        {
            IDbCommand command = connection.CreateCommand();
            command.CommandText = sql.Trim();
            command.CommandType = CommandType.Text;
            command.Parameters.Clear();
            command.CommandText = sql.Trim();
            AddCommandParameters(command, @params);
            return command;
        }

        private void AddCommandParameters(IDbCommand command, Dictionary<string, object> @params)
        {
            if (@params == null)
                return;

            foreach (string key in @params.Keys)
            {
                IDbDataParameter dbParam;

                if (@params[key] is IDbDataParameter)
                {
                    dbParam = (IDbDataParameter)@params[key];
                }
                else
                {
                    dbParam = command.CreateParameter();
                    dbParam.ParameterName = ParameterName(key);
                    dbParam.Value = @params[key];
                }

                if (dbParam.Value == null)
                {
                    dbParam.Value = DBNull.Value;
                }

                command.Parameters.Add(dbParam);
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
