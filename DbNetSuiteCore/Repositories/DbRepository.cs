using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.InkML;
using System.Reflection;

namespace DbNetSuiteCore.Repositories
{
    public class DbRepository : BaseRepository
    {
        private readonly DataSourceType _dataSourceType;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public DbRepository(DataSourceType dataSourceType, IConfiguration configuration, IWebHostEnvironment env)
        {
            _dataSourceType = dataSourceType;
            _configuration = configuration;
            _env = env;
        }

        public IDbConnection GetConnection(string database)
        {
            string? connectionString = _configuration.GetConnectionString(database);
            connectionString = MapDatabasePath(connectionString,_env);

            IDbConnection connection;

            switch (_dataSourceType)
            {
                case DataSourceType.SQlite:
                    connection = new SqliteConnection(connectionString);
                    break;
                case DataSourceType.PostgreSql:
                case DataSourceType.MySql:
                    connection = GetCustomDbConnection(_dataSourceType, connectionString);
                    break;
                default:
                    connection = new SqlConnection(connectionString);
                    break;
            }

            return connection;
        }

        public async Task<DataTable> GetRecords(GridModel gridModel)
        {
            QueryCommandConfig query = gridModel.BuildQuery();
            return await GetDataTable(query, gridModel.ConnectionAlias);
        }

        public async Task<DataTable> GetColumns(GridModel gridModel)
        {
            QueryCommandConfig query = gridModel.BuildEmptyQuery();

            switch (gridModel.DataSourceType)
            {
                case DataSourceType.MSSQL:
                    return await GetSchemaTable(query, gridModel.ConnectionAlias);
                default:
                    return await GetDataTable(query, gridModel.ConnectionAlias);
            }
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
            IDbCommand command = ConfigureCommand(query.Sql, connection, query.Params );
            return await ((DbCommand)command).ExecuteReaderAsync(CommandBehavior.Default);
        }

        public async Task<int> ExecuteNonQuery(CommandConfig commandConfig, IDbConnection connection)
        {
            if (Regex.Match(commandConfig.Sql, "^(delete|update) ", RegexOptions.IgnoreCase).Success)
                if (!Regex.Match(commandConfig.Sql, " where ", RegexOptions.IgnoreCase).Success)
                    throw new Exception("Unqualified updates and deletes are not allowed.");

            IDbCommand command = ConfigureCommand(commandConfig.Sql, connection, commandConfig.Params);
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

        public static string MapDatabasePath(string? connectionString, IWebHostEnvironment env)
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

            if (env != null)
                currentPath = env.WebRootPath;

            string dataSourcePropertyName = "data source";

            connectionString = Regex.Replace(connectionString, dataSourcePropertyName + "=~", dataSourcePropertyName + "=" + currentPath, RegexOptions.IgnoreCase).Replace("=//", "=/");
            return connectionString;
        }

        public static IDbCommand ConfigureCommand(string sql, IDbConnection connection, Dictionary<string, object>? @params = null )
        {
            IDbCommand command = connection.CreateCommand();
            command.CommandText = sql.Trim();
            command.CommandType = CommandType.Text;
            command.Parameters.Clear();
            command.CommandText = sql.Trim();
            AddCommandParameters(command, @params);
            return command;
        }

        public static void AddCommandParameters(IDbCommand command, Dictionary<string, object>? @params)
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

        public static string ParameterName(string key, bool parameterValue = false)
        {
            var temmplate = "@{0}";
            if (key.Length > 0)
                if (temmplate.Substring(0, 1) == key.Substring(0, 1))
                    return key;

            return temmplate.Replace("{0}", CleanParameterName(key));
        }

        public static string CleanParameterName(string key)
        {
            key = Regex.Replace(key, "[^a-zA-Z0-9_]", "_");
            return key;
        
        }

        public static IDbConnection GetCustomDbConnection(DataSourceType dataSourceType, string connectionString)
        {
            Assembly providerAssembly;
            string assemblyName = string.Empty;
            string connectionName = string.Empty;

            switch (dataSourceType)
            {
                case DataSourceType.PostgreSql:
                    assemblyName = "Npgsql";
                    connectionName = "NpgsqlConnection";
                    break;
                case DataSourceType.MySql:
                    assemblyName = "MySqlConnector";
                    connectionName = "MySqlConnection";
                    break;
                default:
                    throw new NotImplementedException($"Custom connection not supported for {dataSourceType} data source type");
            }

            try
            {
                providerAssembly = Assembly.Load(assemblyName);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to load data provider ({assemblyName}). Run Install-Package {assemblyName}. {ex.Message}");
            }
            Type connectionType = providerAssembly.GetType($"{assemblyName}.{connectionName}", true);
            //Type adapterType = ProviderAssembly.GetType(customDataProvider.AdapterTypeName, true);
            //Adapter = (IDbDataAdapter)Activator.CreateInstance(adapterType!);

            Object[] args = new Object[1];
            args[0] = connectionString;

            return (IDbConnection)Activator.CreateInstance(connectionType!, args);
        }
    }
}
