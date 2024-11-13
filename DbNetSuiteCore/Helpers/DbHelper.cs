using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Extensions;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DbNetSuiteCore.Helpers
{
    public static class DbHelper
    {
        public static IDbConnection GetConnection(string connectionAlias, DataSourceType dataSourceType, IConfiguration configuration, IWebHostEnvironment? webHostEnvironment = null)
        {
            string connectionString = GetConnectionString(connectionAlias, configuration);
            IDbConnection connection;

            switch (dataSourceType)
            {
                case DataSourceType.SQLite:
                    connectionString = MapDatabasePath(connectionString, webHostEnvironment!);
                    connection = new SqliteConnection(connectionString);
                    break;
                case DataSourceType.PostgreSql:
                case DataSourceType.MySql:
                    connection = GetCustomDbConnection(dataSourceType, connectionString);
                    break;
                default:
                    connection = new SqlConnection(connectionString);
                    break;
            }

            return connection;
        }

        public static string GetConnectionString(string connectionAlias, IConfiguration configuration)
        {
            return configuration.GetConnectionString(connectionAlias) ?? connectionAlias;
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

            string currentPath = env.WebRootPath;
            string dataSourcePropertyName = "data source";

            connectionString = Regex.Replace(connectionString, dataSourcePropertyName + "=~", dataSourcePropertyName + "=" + currentPath, RegexOptions.IgnoreCase).Replace("=//", "=/");
            return connectionString;
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
                case DataSourceType.Excel:
                    assemblyName = "System.Data.OleDb";
                    connectionName = "OleDbConnection";
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

            Object[] args = new Object[1];
            args[0] = connectionString;

            try
            {
                return (IDbConnection)Activator.CreateInstance(connectionType!, args);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to create <b>{connectionName}</b> connection for connection string or alias <b>{connectionString}</b>");
            }
        }

        public static IDbCommand ConfigureCommand(string sql, IDbConnection connection, Dictionary<string, object>? @params = null, CommandType commandType = CommandType.Text)
        {
            IDbCommand command = connection.CreateCommand();
            command.CommandText = sql.Trim();
            command.CommandType = commandType;
            command.Parameters.Clear();
            command.CommandText = sql.Trim();
            AddCommandParameters(command, @params);
            return command;
        }

        public static string StripColumnRename(string columnExpression)
        {
            string[] columnParts = columnExpression.Split(')');
            columnParts[columnParts.Length - 1] = Regex.Replace(columnParts[columnParts.Length - 1], " as .*", "", RegexOptions.IgnoreCase);
            columnParts[0] = Regex.Replace(columnParts[0], "unique |distinct ", "", RegexOptions.IgnoreCase);

            return String.Join(")", columnParts);
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
            var template = "@{0}";
            if (key.Length > 0)
                if (template.Substring(0, 1) == key.Substring(0, 1))
                    return key;

            return template.Replace("{0}", CleanParameterName(key));
        }

        public static string CleanParameterName(string key)
        {
            key = Regex.Replace(key, "[^a-zA-Z0-9_]", "_");
            return key;
        }

        public static Dictionary<string, string> GetConnections(IConfiguration configuration)
        {
            var connectionStrings = new Dictionary<string, string>();
            configuration.GetSection("ConnectionStrings").Bind(connectionStrings);
            return connectionStrings;
        }

        public static List<string> GetDatabases(string connectionAlias, IConfiguration configuration)
        {
            string connectionString = GetConnectionString(connectionAlias, configuration);
            var client = new MongoClient(connectionString);
            return client.ListDatabaseNames().ToList();
        }

        public static List<string> GetTables(string connectionAlias, IConfiguration configuration, string database)
        {
            string connectionString = GetConnectionString(connectionAlias, configuration);
            var client = new MongoClient(connectionString);
            return client.GetDatabase(database).ListCollectionNames().ToList();
        }

        public static List<string> GetTables(string connectionAlias, DataSourceType dataSourceType, IConfiguration configuration, IWebHostEnvironment? webHostEnvironment = null)
        {
            List<string> tables = new List<string>();

            var connection = GetConnection(connectionAlias, dataSourceType, configuration, webHostEnvironment);
            connection.Open();
            switch (dataSourceType)
            {
                case DataSourceType.MSSQL:
                    tables = LoadMSSQLTables(connection as SqlConnection);
                    break;
                default:
                    tables = LoadSchemaTables(dataSourceType, connection);
                    break;
            }

            connection.Close();

            return tables;
        }
        public static string QualifyExpression(string expression, DataSourceType dataSourceType, bool userDefined = false)
        {
            if (QualifyTemplate(dataSourceType) == "@")
            {
                return expression;
            }

            if (expression.Substring(0, 1) == QualifyTemplate(dataSourceType).Substring(0, 1))
            {
                return expression;
            }

            if (expression.Substring(0, 1) == QualifyTemplate(dataSourceType).Substring(0, 1))
            {
                return expression;
            }

            if (userDefined && TextHelper.IsAlphaNumeric(expression))
            {
                return expression;
            }

            return QualifyTemplate(dataSourceType).Replace("@", expression);
        }

        public static string QualifyTemplate(DataSourceType dataSourceType)
        {
            switch (dataSourceType)
            {
                case DataSourceType.MSSQL:
                case DataSourceType.Excel:
                case DataSourceType.SQLite:
                    return $"[@]";
                case DataSourceType.MySql:
                    return $"`@`";
                case DataSourceType.PostgreSql:
                    return $"\"@\"";
            }
            return "@";
        }


        private static List<string> LoadMSSQLTables(SqlConnection connection)
        {
            var schemaTable = connection.GetSchema("Tables").Select(string.Empty, "TABLE_SCHEMA,TABLE_NAME").CopyToDataTable();

            List<string> tables = new List<string>();

            foreach (DataRow dataRow in schemaTable.Rows)
            {
                tables.Add($"{dataRow[1]}.[{dataRow[2]}]");
            }

            return tables;
        }

        private static List<string> LoadSchemaTables(DataSourceType dataSourceType, IDbConnection connection)
        {
            List<string> tables = new List<string>();
            var sql = string.Empty;
            switch (dataSourceType)
            {
                case DataSourceType.SQLite:
                    sql = "SELECT name FROM sqlite_master WHERE type in ('table','view') order by 1";
                    break;
                case DataSourceType.PostgreSql:
                    sql = "SELECT table_schema || '.' || table_name AS name  FROM information_schema.tables where table_schema = 'public' order by 1";
                    break;
                case DataSourceType.MySql:
                    sql = "SELECT CONCAT(`table_schema`,'.',`table_name`) AS name  FROM information_schema.tables order by 1";
                    break;
            }
            var command = ConfigureCommand(sql, connection);

            DataTable schemaTable = new DataTable();
            schemaTable.Load(command.ExecuteReader(CommandBehavior.Default));

            foreach (DataRow dataRow in schemaTable.Rows)
            {
                tables.Add($"{dataRow[0]}");
            }

            if (dataSourceType == DataSourceType.SQLite)
            {
                tables = tables.Select(t => QualifyExpression(t, DataSourceType.SQLite)).ToList();
            }

            return tables;
        }
        private static void LoadMongoDBCollections(IMongoDatabase database, List<string> tables)
        {
            tables = database.ListCollectionNames().ToList();
        }
    }
}