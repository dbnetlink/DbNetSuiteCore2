using DbNetTimeCore.Enums;
using DbNetTimeCore.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using DataRow = System.Data.DataRow;

namespace DbNetTimeCore.Utilities
{
    public class DbNetDataCore : IDisposable
    {
        private readonly Hashtable _reservedWords = new Hashtable();
        private readonly DataTable _dataTable;

        public enum MetaDataType
        {
            MetaDataCollections,
            Columns,
            Databases,
            DataSourceInformation,
            DataTypes,
            ForeignKeys,
            Functions,
            IndexColumns,
            Indexes,
            PrimaryKeys,
            Procedures,
            ProcedureParameters,
            Restrictions,
            ReservedWords,
            Tables,
            Views,
            ViewColumns,
            UserDefinedTypes,
            Users,
            UserTables,
            UserViews
        }
        public class CommandConfig
        {
            public string Sql = String.Empty;
            public Dictionary<string,object> Params = new Dictionary<string, object>();

            public CommandConfig()
                : this("")
            {
            }

            public CommandConfig(string sql)
            {
                this.Sql = sql;
            }
            public CommandConfig(string sql, Dictionary<string, object> parameters)
            {
                this.Sql = sql;
                this.Params = parameters;
            }
        }
        public class QueryCommandConfig : CommandConfig
        {
            public CommandBehavior Behavior = CommandBehavior.Default;

            public QueryCommandConfig()
                : this("")
            {
            }
            public QueryCommandConfig(string sql)
                : base(sql)
            {
            }
            public QueryCommandConfig(string sql, Dictionary<string, object> parameters) : base(sql, parameters)
            {
            }
        }
        public class UpdateCommandConfig : CommandConfig
        {
            public Dictionary<string, object> FilterParams = new Dictionary<string, object>();

            public UpdateCommandConfig()
                : this("")
            {
            }
            public UpdateCommandConfig(string Sql)
                : base(Sql)
            {
            }
        }
        public DatabaseType Database { get; }
        private IWebHostEnvironment Env { get; }
        private string ConnectionString { get; }
        public DataProvider Provider { get; }
        public Assembly ProviderAssembly { get; }
        public IDbDataAdapter Adapter;
        public IDbConnection Connection;
        public IDbCommand Command;
        public IDataReader Reader;
        public IDbTransaction Transaction;
        public bool VerboseErrorInfo = true;
        public bool SummaryExceptionMessage = false;
        public bool ShowConnectionStringOnError = false;
        public bool CloseConnectionOnError = true;
        public string NameDelimiterTemplate = "{0}";
        public int CommandTimeout = -1;
        public string ParameterTemplate = "@{0}";
        public bool ConvertEmptyToNull = true;

        public Hashtable ReservedWords => GetReservedWords();
        public string DatabaseName => Connection?.Database;

        public DbNetDataCore(string connectionString, IWebHostEnvironment env, IConfiguration configuration, DataProvider? dataProvider)
            : this(configuration.GetConnectionString(connectionString), DeriveProvider(configuration.GetConnectionString(connectionString), dataProvider), env)
        {

        }

        public DbNetDataCore(string connectionString, IWebHostEnvironment env, IConfiguration configuration, DatabaseType databaseType)
    : this(configuration.GetConnectionString(connectionString), DeriveProvider(databaseType), env)
        {

        }
        public DbNetDataCore(DataTable dataTable): this(string.Empty, DataProvider.DataTable, null)
        {
            _dataTable = dataTable;
        }

        public DbNetDataCore(string connectionString, IWebHostEnvironment env)
            : this(connectionString, DeriveProvider(connectionString), env)
        {

        }

        public DbNetDataCore(IWebHostEnvironment env)
            : this("Data Source=InMemory;Mode=Memory;Cache=Private;", DataProvider.SQLite, env)
        {
        }

        public DbNetDataCore(string connectionString, DataProvider dataProvider, IWebHostEnvironment env)
        {
            Env = env;
            Provider = dataProvider;
            ConnectionString = MapDatabasePath(connectionString);
            CustomDataProvider customDataProvider = null;

            try
            {
                switch (Provider)
                {
                    case DataProvider.SQLite:
                        Connection = new SqliteConnection(ConnectionString);
                        ProviderAssembly = Assembly.GetAssembly(typeof(SqliteConnection));
                        Database = DatabaseType.SQLite;
                        break;
                    case DataProvider.SqlClient:
                        Connection = new SqlConnection(ConnectionString);
                        Adapter = new SqlDataAdapter();
                        Database = DatabaseType.MSSqlServer;
                        ProviderAssembly = Assembly.GetAssembly(typeof(SqlConnection));
                        break;
                    case DataProvider.MySql:
                        customDataProvider = new CustomDataProvider("MySql.Data", "MySqlClient.MySqlConnection");
                        Database = DatabaseType.MySQL;
                        break;
                    case DataProvider.Npgsql:
                        customDataProvider = new CustomDataProvider("Npgsql", "NpgsqlConnection");
                        Database = DatabaseType.PostgreSQL;
                        break;
                    case DataProvider.MySqlConnector:
                        customDataProvider = new CustomDataProvider("MySqlConnector", "MySqlConnection");
                        Database = DatabaseType.MySQL;
                        break;
                    case DataProvider.DataTable:
                        Database = DatabaseType.SQLite;
                        break;
                }
            }
            catch (Exception Ex)
            {
                HandleError(Ex);
            }

            switch (Database)
            {
                case DatabaseType.MSSqlServer:
                case DatabaseType.SQLite:
                    NameDelimiterTemplate = "[{0}]";
                    break;
                case DatabaseType.PostgreSQL:
              //  case DatabaseType.Oracle:
                    NameDelimiterTemplate = "\"{0}\"";
                    break;
                case DatabaseType.MySQL:
                    NameDelimiterTemplate = "`{0}`";
                    break;
            }

            if (customDataProvider != null)
            {
                try
                {
                    ProviderAssembly = Assembly.Load(customDataProvider.AssemblyName);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to load data provider ({customDataProvider.AssemblyName}). Run Install-Package {customDataProvider.PackageName}. {ex.Message}");
                }
                Type connectionType = ProviderAssembly.GetType(customDataProvider.ConnectionTypeName, true);
                Type adapterType = ProviderAssembly.GetType(customDataProvider.AdapterTypeName, true);

                Object[] args = new Object[1];
                args[0] = ConnectionString;

                Connection = (IDbConnection)Activator.CreateInstance(connectionType!, args);
                Adapter = (IDbDataAdapter)Activator.CreateInstance(adapterType!);
            }

            if (Connection != null)
            {
                Command = Connection.CreateCommand();

                if (Adapter != null)
                {
                    Adapter.SelectCommand = Command;
                }
            }
        }
     

        public void Open()
        {
            if (Connection.State != ConnectionState.Open)
            {
                try
                {
                    Connection.Open();
                }
                catch (Exception ex)
                {
                    HandleError(ex);
                }
            }
        }
        public void ExecuteQuery(CommandConfig cmdConfig)
        {
            ExecuteQuery(cmdConfig.Sql, cmdConfig.Params);
        }

        public void ExecuteQuery(QueryCommandConfig cmdConfig)
        {
            ExecuteQuery(cmdConfig.Sql, cmdConfig.Params, cmdConfig.Behavior);
        }

        public void ExecuteQuery(string sql)
        {
            ExecuteQuery(sql, new ListDictionary(), CommandBehavior.Default);
        }

        public void ExecuteQuery(string sql, IDictionary @params)
        {
            ExecuteQuery(sql, @params, CommandBehavior.Default);
        }

        public void ExecuteQuery(string sql, IDictionary @params, CommandBehavior behaviour)
        {
            if (sql.ToLower().IndexOf("select ") == 0)
                if (sql.ToLower().IndexOf(" where ") == -1 && @params.Count > 0)
                    sql = AddWhereClause(sql, @params);

            ConfigureCommand(sql, @params);

            try
            {
                Reader = Command.ExecuteReader(behaviour);
            }
            catch (Exception Ex)
            {
                HandleError(Ex);
            }

        }


        public int ExecuteNonQuery(CommandConfig commandConfig)
        {
            if (Regex.Match(commandConfig.Sql, "^(delete|update) ", RegexOptions.IgnoreCase).Success)
                if (!Regex.Match(commandConfig.Sql, " where ", RegexOptions.IgnoreCase).Success)
                    throw new Exception("Unqualified updates and deletes are not allowed.");

            ConfigureCommand(commandConfig.Sql, commandConfig.Params);
            int RetVal = 0;

            try
            {
                RetVal = Command.ExecuteNonQuery();
            }
            catch (Exception Ex)
            {
                HandleError(Ex);
            }

            return RetVal;
        }

        public long ExecuteScalar(CommandConfig commandConfig)
        {
            ConfigureCommand(commandConfig.Sql, commandConfig.Params);
            long id = -1;

            try
            {
                id = Int64.Parse(Command.ExecuteScalar().ToString());
            }
            catch (Exception Ex)
            {
                HandleError(Ex);
            }

            return id;
        }
        public long ExecuteInsert(CommandConfig commandConfig, string autoIncrementColumnName)
        {
            long identity = -1;
            try
            {
                if (this.Database == DatabaseType.PostgreSQL && autoIncrementColumnName != null)
                {
                    commandConfig.Sql = $"{commandConfig.Sql} returning {autoIncrementColumnName}";
                    identity = this.ExecuteScalar(commandConfig);
                }
                else
                {
                    this.ExecuteNonQuery(commandConfig);
                    identity = GetAutoIncrementValue();
                }
            }
            catch (Exception Ex)
            {
                HandleError(Ex);
            }

            return identity;
        }

        private long GetAutoIncrementValue()
        {
            long id = -1;

            string Sql = "";
            switch (this.Database)
            {
                case DatabaseType.MySQL:
                case DatabaseType.MSSqlServer:
                    Sql = "SELECT @@IDENTITY";
                    break;
                case DatabaseType.SQLite:
                    Sql = "SELECT last_insert_rowid()";
                    break;
            }

            Command.CommandText = Sql;
            id = Int64.Parse(Command.ExecuteScalar().ToString());

            return id;
        }
        public string UpdateConcatenationOperator(string expr)
        {
            switch (Database)
            {
                case DatabaseType.SQLite:
                    expr = expr.Replace("+", "||");
                    break;
            }

            return expr;
        }

        public ListDictionary ParseParameters(string sql)
        {
            ListDictionary parameters = new ListDictionary();
            MatchCollection mc = Regex.Matches(sql, @"[@:](\w*)");

            foreach (Match m in mc)
                parameters[m.Groups[1].Value] = new object();

            return parameters;
        }

        public ListDictionary DeriveParameters(string procedureName)
        {
            string typeName = "";

            switch (Provider)
            {
                case DataProvider.SqlClient:
                    typeName = "SqlCommandBuilder";
                    break;
                case DataProvider.MySql:
                case DataProvider.MySqlConnector:
                    typeName = "MySqlCommandBuilder";
                    break;
                case DataProvider.Npgsql:
                    typeName = "NpgsqlCommandBuilder";
                    break;

                default:
                    throw new Exception("DeriveParameters not supported by this provider");
            }

            string[] typeNameParts = Connection.GetType().FullName.Split('.');

            typeNameParts[typeNameParts.Length - 1] = typeName;
            string commandBuilderTypeName = string.Join(".", typeNameParts);

            Type commandBuilder = ProviderAssembly.GetType(commandBuilderTypeName, true);

            Type[] typeArray = new Type[1];
            typeArray.SetValue(ProviderAssembly.GetType(commandBuilderTypeName.Replace("Builder", ""), true), 0);

            MethodInfo mi = commandBuilder.GetMethod("DeriveParameters", typeArray);

            if (mi == null)
                throw new Exception(
                    $"Method --> DeriveParameters not supported by --> {string.Join(".", typeNameParts)} {commandBuilder.GetType()}");

            Object[] args = new Object[1];

            Command.CommandText = procedureName;
            Command.CommandType = CommandType.StoredProcedure;

            args[0] = Command;

            mi.Invoke(Connection, args);

            ListDictionary parameters = new ListDictionary();

            foreach (IDbDataParameter dbParam in Command.Parameters)
                if (dbParam.Direction != ParameterDirection.ReturnValue)
                    parameters.Add(Regex.Replace(dbParam.ParameterName, "[@:?]", string.Empty), dbParam);

            return parameters;
        }

        public void SetParamValue(ListDictionary @params, string paramName, object paramValue)
        {
            bool found = false;
            foreach (string k in @params.Keys)
            {
                if (k.ToLower() == paramName.ToLower())
                {
                    if (@params[k] is IDbDataParameter)
                        ((IDbDataParameter)@params[k]).Value = paramValue;
                    else
                        @params[k] = paramValue;
                    found = true;
                    break;
                }
            }

            if (!found)
                @params[ParameterName(paramName)] = paramValue;
        }
        public string ParameterName(string key, bool parameterValue = false)
        {
            if (key.Length > 0 && ParameterTemplate.Length > 1)
                if (ParameterTemplate.Substring(0, 1) == key.Substring(0, 1))
                    return key;
            /*
            if (parameterValue && Database == DatabaseType.MySQL)
            {
                return CleanParameterName(key);
            }
            */
            return ParameterTemplate.Replace("{0}", CleanParameterName(key));
        }
        public DataTable GetSchemaTable(string sql)
        {
            string[] tableName = new string[0];
            if (sql.ToLower().IndexOf("select ") != 0)
            {
                tableName = sql.Split('.');
                sql = $"select * from {QualifiedDbObjectName(sql)} where 1=2";
            }

            ExecuteQuery(sql, new ListDictionary(), CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo);
            DataTable dataTable = Reader.GetSchemaTable();

            if (dataTable == null)
                return null;

            if (!dataTable.Columns.Contains("DataTypeName"))
                dataTable.Columns.Add(new System.Data.DataColumn("DataTypeName", typeof(string)));

            dataTable.Columns.Add(new System.Data.DataColumn("FieldTypeName", typeof(string)));
            dataTable.Columns.Add(new System.Data.DataColumn("ProviderFieldTypeName", typeof(string)));
            dataTable.Columns.Add(new System.Data.DataColumn("DefaultValue", typeof(string)));

            dataTable.Columns["DataTypeName"].ReadOnly = false;

            for (int i = 0; i < Reader.FieldCount; i++)
            {
                var dataTypeName = Reader.GetDataTypeName(i);
                var fieldType = Reader.GetFieldType(i);
                dataTable.Rows[i]["DataTypeName"] = dataTypeName; ;
                dataTable.Rows[i]["FieldTypeName"] = fieldType?.ToString();

                switch (Reader.GetType().Name)
                {
                    case "SqlDataReader":
                        Object[] args = new Object[1];
                        args[0] = i;
                        var providerFieldTypeName = InvokeMethod(Reader, "GetProviderSpecificFieldType", args);
                        dataTable.Rows[i]["ProviderFieldTypeName"] = (providerFieldTypeName == null ? string.Empty : providerFieldTypeName.ToString()) ?? string.Empty;
                        break;
                }

            }

            Reader.Close();

            if (Database == DatabaseType.MSSqlServer && tableName.Length > 0)
            {
                sql =
                    $"SELECT K.COLUMN_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS T INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE K ON T.CONSTRAINT_NAME = K.CONSTRAINT_NAME WHERE T.CONSTRAINT_TYPE = 'PRIMARY KEY' AND T.TABLE_NAME = '{tableName[tableName.Length - 1]}'";

                if (tableName.Length == 2)
                {
                    sql += $" AND T.TABLE_SCHEMA = '{tableName[0]}'";
                }

                ExecuteQuery(sql);
                while (Reader.Read())
                {
                    if (dataTable.Columns["IsKey"].ReadOnly)
                    {
                        dataTable.Columns["IsKey"].ReadOnly = false;
                        foreach (DataRow R in dataTable.Rows)
                            R["IsKey"] = false;
                    }
                    DataRow[] Rows = dataTable.Select($"ColumnName = '{Reader.GetString(0)}'");
                    Rows[0]["IsKey"] = true;
                }

                sql =
                    $"SELECT COLUMN_NAME, COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName[tableName.Length - 1]}'";

                if (tableName.Length == 2)
                {
                    sql += $" AND TABLE_SCHEMA = '{tableName[0]}'";
                }

                DataTable defaultValues = GetDataTable(sql);

                foreach (DataRow row in defaultValues.Rows)
                {
                    DataRow[] rows = dataTable.Select($"ColumnName = '{row["COLUMN_NAME"]}'");

                    if (rows.Length > 0)
                    {
                        rows[0]["DefaultValue"] = row["COLUMN_DEFAULT"];
                    }
                }
            }

            if (Database == DatabaseType.MSSqlServer)
            {
                sql = $"SELECT COLUMN_NAME, COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{dataTable.Rows[0]["BaseTableName"]}'";

                if (dataTable.Rows[0]["BaseSchemaName"] != null && dataTable.Rows[0]["BaseSchemaName"].ToString() != String.Empty)
                {
                    sql += $" AND TABLE_SCHEMA = '{dataTable.Rows[0]["BaseSchemaName"]}'";
                }

                DataTable defaultValues = GetDataTable(sql);

                foreach (DataRow row in defaultValues.Rows)
                {
                    DataRow[] rows = dataTable.Select($"ColumnName = '{row["COLUMN_NAME"]}'");

                    if (rows.Length > 0)
                    {
                        rows[0]["DefaultValue"] = row["COLUMN_DEFAULT"];
                    }
                }
            }

            switch (Database)
            {
                case DatabaseType.MySQL:
                    dataTable.Columns["ColumnSize"].ReadOnly = false;

                    foreach (DataRow Row in dataTable.Rows)
                    {
                        if (Row["ColumnSize"].ToString() != "-1")
                            continue;

                        switch (Row["ProviderType"].ToString())
                        {
                            case "749":
                            case "750":
                            case "751":
                            case "752":
                                Row["ColumnSize"] = Int32.MaxValue;
                                break;
                        }
                    }
                    break;
              
                case DatabaseType.PostgreSQL:
                    dataTable.Columns["ColumnSize"].ReadOnly = false;

                    foreach (DataRow Row in dataTable.Rows)
                    {
                        if (Row["ColumnSize"].ToString() == "-1")
                            Row["ColumnSize"] = Int32.MaxValue;
                    }
                    break;
            }

            return dataTable;
        }

        public List<DbObject> InformationSchema(MetaDataType collectionType)
        {
            List<DbObject> dbObjects = new List<DbObject>();

            switch (collectionType)
            {
                case MetaDataType.Tables:
                    switch (Database)
                    {
                        case DatabaseType.SQLite:
                            dbObjects = SqliteMasterQuery("table");
                            break;
                        case DatabaseType.MySQL:
                            dbObjects = MySqlInformationSchemaQuery("BASE TABLE");
                            break;
                        case DatabaseType.PostgreSQL:
                            dbObjects = PostGresInformationSchemaQuery("BASE TABLE");
                            break;
                        default:
                            dbObjects = InformationSchemaQuery("BASE TABLE");
                            break;
                    }
                    break;
                case MetaDataType.Views:
                    switch (Database)
                    {
                        case DatabaseType.SQLite:
                            dbObjects = SqliteMasterQuery("view");
                            break;
                        case DatabaseType.MySQL:
                            dbObjects = MySqlInformationSchemaQuery("VIEW");
                            break;
                        case DatabaseType.PostgreSQL:
                            dbObjects = PostGresInformationSchemaQuery("VIEW");
                            break;
                        default:
                            dbObjects = InformationSchemaQuery("VIEW");
                            break;
                    }
                    break;
            }

            return dbObjects;
        }

        public DataTable MetaDataCollection(MetaDataType collectionType)
        {
            string getSchemaArg = collectionType.ToString();

            switch (collectionType)
            {
                case MetaDataType.UserTables:
                    getSchemaArg = "Tables";
                    break;
                case MetaDataType.UserViews:
                    getSchemaArg = "Views";
                    break;
                case MetaDataType.Functions:
                    getSchemaArg = "Procedures";
                    break;
            }

            Object t = new Object();


            if (!(t is DataTable))
            {
                Object[] Args = new Object[1];
                Args[0] = getSchemaArg;

                t = InvokeMethod(Connection, "GetSchema", Args);
            }

            if (t is DataTable)
            {
                DataTable schema = (DataTable)t;
                switch (collectionType)
                {
                    case MetaDataType.DataTypes:
                        RemapDataTypesSchemaColumnNames(schema);
                        break;
                    case MetaDataType.Tables:
                    case MetaDataType.Views:
                        RemapTablesSchemaColumnNames(schema);
                        break;
                    case MetaDataType.Procedures:
                    case MetaDataType.Functions:
                        switch (Database)
                        {
                            case DatabaseType.MSSqlServer:
                                DataRow[] PRows = schema.Select("ROUTINE_TYPE = '" + collectionType.ToString().ToUpper().Replace("S", "") + "'");
                                DataTable PT = schema.Clone();
                                foreach (DataRow R in PRows)
                                    PT.ImportRow(R);
                                schema = PT;
                                break;
                        }
                        break;
                }

                return schema;
            }
            return new DataTable();
        }


        public string QualifiedDbObjectName(string objectName, bool split = true)
        {
            string[] nameParts = split ? objectName.Split('.') : new[] { objectName };

            Regex re = new Regex(NameDelimiterTemplate.Replace("[", @"\[").Replace("]", @"\]").Replace("{0}", ".*"));

            if (Provider != DataProvider.DataTable)
            {
                for (int I = 0; I < nameParts.Length; I++)
                    if (Regex.IsMatch(nameParts[I], @"\W") || IsReservedWord(nameParts[I]) || StartsWithNumeric(nameParts[I]))
                        if (!re.IsMatch(nameParts[I]))
                            nameParts[I] = NameDelimiterTemplate.Replace("{0}", nameParts[I].Trim());
            }

            return string.Join(".", nameParts);
        }

        public string UnqualifiedDbObjectName(string objectName)
        {
            return UnqualifiedDbObjectName(objectName, false);
        }

        public string UnqualifiedDbObjectName(string objectName, bool baseNameOnly)
        {
            string[] NameParts = objectName.Split('.');

            Regex RE = new Regex(@"[\[\]\`""]");

            for (int I = 0; I < NameParts.Length; I++)
                NameParts[I] = RE.Replace(NameParts[I], "");

            if (baseNameOnly)
                return NameParts[NameParts.Length - 1];
            return string.Join(".", NameParts);
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

            if (Env != null)
                currentPath = Env.WebRootPath;

            string dataSourcePropertyName = "data source";

            connectionString = Regex.Replace(connectionString, dataSourcePropertyName + "=~", dataSourcePropertyName + "=" + currentPath, RegexOptions.IgnoreCase).Replace("=//", "=/");
            return connectionString;
        }

        private static DataProvider DeriveProvider(DatabaseType databaseType)
        {
            switch(databaseType)
            {
                case DatabaseType.MySQL:
                case DatabaseType.MariaDB:
                    return DataProvider.MySqlConnector;
                case DatabaseType.PostgreSQL:
                    return DataProvider.Npgsql;
                case DatabaseType.SQLite:
                    return DataProvider.SQLite;
                default:
                    return DataProvider.SqlClient;
            }
        }

        public static DatabaseType DeriveDatabaseType(string connectionString)
        {
            DataProvider dataProvider = DeriveProvider(connectionString);

            switch (dataProvider)
            {
                case DataProvider.MySql:
                     return DatabaseType.MySQL;
                case DataProvider.Npgsql:
                    return DatabaseType.PostgreSQL;
                case DataProvider.SQLite:
                    return DatabaseType.SQLite;
                default:
                    return DatabaseType.MSSqlServer;
            }
        }
        private static DataProvider DeriveProvider(string connectionString, DataProvider? dataProvider = null)
        {
            if (dataProvider.HasValue)
            {
                return dataProvider.Value;
            }

            if (!connectionString.EndsWith(";"))
                connectionString += ";";

            if (Regex.IsMatch(connectionString, @"Data Source=(.*)\.db;", RegexOptions.IgnoreCase))
                return DataProvider.SQLite;

            if (Regex.IsMatch(connectionString, "port=3306;", RegexOptions.IgnoreCase))
                return DataProvider.MySql;

            if (Regex.IsMatch(connectionString, "port=5432;", RegexOptions.IgnoreCase))
                return DataProvider.Npgsql;

            return ExtractDataProvider(connectionString);
        }

        public DataTable GetDataTable(string sql)
        {
            return GetDataTable(new QueryCommandConfig(sql));
        }

        public string CleanParameterName(string key)
        {
            key = Regex.Replace(key, "[^a-zA-Z0-9_]", "_");
            return key;
        }

        public DataTable GetDataTable(QueryCommandConfig cmdConfig)
        {
            DataTable dataTable = new DataTable();
            ExecuteQuery(cmdConfig);
            dataTable.Load(Reader);
            return dataTable;
        }

        private void ConfigureCommand(string sql, IDictionary @params)
        {
            CloseReader();
            Command.CommandText = sql.Trim();
            Command.CommandType = GetCommandType(Command.CommandText);

            Command.Parameters.Clear();
            AddCommandParameters(@params);
        }

        private void AddCommandParameters(IDictionary @params)
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
                    dbParam = Command.CreateParameter();
                    dbParam.ParameterName = ParameterName(key);
                    dbParam.Value = @params[key];
                }

                if (dbParam.Value == null)
                {
                    dbParam.Value = DBNull.Value;
                }

                Command.Parameters.Add(dbParam);
            }
        }

        private bool StartsWithNumeric(string token)
        {
            return Regex.IsMatch(token, @"^\d");
        }
        private void RemapDataTypesSchemaColumnNames(DataTable schema)
        {
            switch (Database)
            {
               // case DatabaseType.Oracle:
                case DatabaseType.MySQL:
                    foreach (DataRow row in schema.Rows)
                    {
                        if (row["CreateParameters"].ToString() == "size")
                            row["CreateParameters"] = "length";
                    }
                    break;
            }
        }

        private void RemapTablesSchemaColumnNames(DataTable Schema)
        {
            Hashtable ColumnRemappings = new Hashtable();

            ColumnRemappings["TABLE_SCHEMA"] = new[] { "OWNER", "SCHEMA", "TABLE_SCHEM", "TABLE_OWNER" };
            ColumnRemappings["TABLE_TYPE"] = new[] { "TABLETYPE", "TYPE" };
            ColumnRemappings["TABLE_NAME"] = new[] { "NAME", "VIEW_NAME" };

            foreach (string Key in ColumnRemappings.Keys)
                foreach (string ColumnName in ((string[])ColumnRemappings[Key]))
                    if (Schema.Columns.Contains(ColumnName))
                    {
                        Schema.Columns[ColumnName].ColumnName = Key;
                        break;
                    }
        }
        private CommandType GetCommandType(string commandText)
        {
            if (Regex.Match(commandText, "^(alter|drop|create|select|insert|delete|update|set|if|begin|print|open) ", RegexOptions.IgnoreCase).Success)
                return CommandType.Text;
            return CommandType.StoredProcedure;
        }

        private void CloseReader()
        {
            if (Reader is IDataReader)
            {
                if (!Reader.IsClosed)
                {
                    try
                    {
                        Command.Cancel();
                    }
                    catch (Exception)
                    {
                    }

                    Reader.Close();
                }
            }
        }
        private static DataProvider ExtractDataProvider(string cs)
        {
            if (!cs.EndsWith(";"))
                cs += ";";

            if (!Regex.IsMatch(cs, @"DataProvider=(.*?);", RegexOptions.IgnoreCase))
                return DataProvider.SqlClient;

            Match m = Regex.Match(cs, @"DataProvider=(.*?);", RegexOptions.IgnoreCase);

            foreach (DataProvider p in Enum.GetValues(typeof(DataProvider)))
                if (p.ToString().ToLower() == m.Groups[1].Value.ToLower())
                    return p;

            return DataProvider.SqlClient;
        }

        private void HandleError(Exception ex)
        {
            StackTrace t = new StackTrace(1);
            StackFrame f = t.GetFrame(0);
            string methodName = f.GetMethod().DeclaringType.FullName + "." + f.GetMethod().Name;

            string msg = ex.Message + Environment.NewLine + Environment.NewLine;
            string exMsg = msg;

            if (ex.InnerException != null)
            {
                string s = ex.InnerException.Message + Environment.NewLine + Environment.NewLine;
                msg += s;
                exMsg += $"({s})";
            }

            msg += "--> Method: " + methodName + Environment.NewLine;
            if (ProviderAssembly != null)
                msg += "--> Provider: " + ProviderAssembly.FullName + Environment.NewLine;

            if (VerboseErrorInfo)
            {
                msg += CommandErrorInfo();
            }
            else
            {
                if (Connection != null)
                    msg += "For more information set the VerboseErrorInfo property";
            }


            if (SummaryExceptionMessage)
                throw new Exception(exMsg);
            throw new Exception(exMsg, new Exception(msg));
        }


        private string CommandErrorInfo()
        {
            string msg = string.Empty;

            if (!VerboseErrorInfo)
                return string.Empty;

            if (Connection == null)
            {
                if (ShowConnectionStringOnError)
                    msg += $"--> Connection: {ConnectionString}{Environment.NewLine}";
            }
            else
            {
                if (CloseConnectionOnError)
                    Connection.Close();

                if (ShowConnectionStringOnError)
                    msg += "--> Connection: " + Connection.ConnectionString + Environment.NewLine;

                msg += $"--> Command: {Command.CommandText}{Environment.NewLine}";
                msg += $"--> Type: {Command.CommandType}{Environment.NewLine}";
                msg += $"--> Timeout: {Command.CommandTimeout}{Environment.NewLine}";

                if (Command.Parameters.Count > 0)
                    msg += "--> Parameters: " + ParameterList() + Environment.NewLine;
            }

            return msg;
        }

        private string AddWhereClause(string sql, IDictionary @params)
        {
            if (@params.Count == 0)
                return sql;

            return sql + " where " + BuildParamFilter(@params);
        }

        private string BuildParamFilter(IDictionary @params)
        {
            if (@params.Count == 0)
                return "";

            List<string> parameters = new List<string>();

            foreach (string key in @params.Keys)
                parameters.Add($"{QualifiedDbObjectName(key)} = {ParameterName(key)}");

            return $"({string.Join(" and ", parameters.ToArray())})";
        }

        public bool IsReservedWord(string token)
        {
            return (ReservedWords[token.ToUpper()] != null);
        }
        private Hashtable GetReservedWords()
        {
            if (_reservedWords.Count > 0)
                return _reservedWords;

            DataTable words = MetaDataCollection(MetaDataType.ReservedWords);

            foreach (DataRow row in words.Rows)
            {
                if (row[0] != null)
                    _reservedWords[row[0].ToString()?.ToUpper() ?? string.Empty] = true;
            }

            if (_reservedWords.Count == 0)
            {
                _reservedWords[string.Empty] = true;
            }
            return _reservedWords;
        }

        private string ParameterList()
        {
            ArrayList parameters = new ArrayList();

            foreach (IDbDataParameter parameter in Command.Parameters)
                parameters.Add(parameter.ParameterName + "=" + ((parameter.Value == null) || parameter.Value == DBNull.Value ? "NULL" : parameter.Value.ToString()));
            return string.Join(",", (string[])parameters.ToArray(typeof(string)));
        }


        private Object InvokeMethod(object instance, string methodName)
        {
            return InvokeMethod(instance, methodName, new Object[0]);
        }


        private Object InvokeMethod(object instance, string methodName, Object[] args)
        {
            string message;
            return InvokeMethod(instance, methodName, args, out message);
        }

        private List<DbObject> InformationSchemaQuery(string tableType)
        {
            var sql = $"SELECT TABLE_SCHEMA,TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='{tableType}' order by TABLE_SCHEMA, TABLE_NAME";
            var dataTable = GetDataTable(sql);
            return dataTable.Rows.Cast<DataRow>().Select(r => new DbObject { QualifiedTableName = $"[{r[0]}].[{r[1]}]", TableName = $"{r[0]}.{r[1]}" }).ToList();
        }
        private List<DbObject> MySqlInformationSchemaQuery(string tableType)
        {
            var sql = $"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='{tableType}' and TABLE_SCHEMA = '{DatabaseName.ToLower()}' order by TABLE_NAME";
            var dataTable = GetDataTable(sql);
            return dataTable.Rows.Cast<DataRow>().Select(r => new DbObject { QualifiedTableName = r[0].ToString(), TableName = r[0].ToString() }).ToList();
        }

        private List<DbObject> PostGresInformationSchemaQuery(string tableType)
        {
            var sql = $"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='{tableType}' and table_catalog = '{DatabaseName.ToLower()}' and table_schema = 'public' order by TABLE_NAME";
            var dataTable = GetDataTable(sql);
            return dataTable.Rows.Cast<DataRow>().Select(r => new DbObject { QualifiedTableName = r[0].ToString(), TableName = r[0].ToString() }).ToList();
        }
        private List<DbObject> SqliteMasterQuery(string tableType)
        {
            var sql = $"SELECT name FROM sqlite_master WHERE type='{tableType}' and name not like 'sqlite_%' order by name";
            var dataTable = GetDataTable(sql);
            return dataTable.Rows.Cast<DataRow>().Select(r => new DbObject { QualifiedTableName = r[0].ToString(), TableName = r[0].ToString() }).ToList();
        }

        private string ForeignKeyInformation()
        {
            return "SELECT \r\n    KCU1.TABLE_NAME AS TABLE_NAME \r\n    ,KCU1.COLUMN_NAME AS COLUMN_NAME \r\n    ,KCU2.TABLE_NAME AS FK_TABLE_NAME \r\n    ,KCU2.COLUMN_NAME AS FK_KEY_COLUMN_NAME \r\n\t,TEXT_COLUMN.COLUMN_NAME AS FK_TEXT_COLUMN_NAME\r\nFROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS RC \r\n\r\nINNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KCU1 \r\n    ON KCU1.CONSTRAINT_CATALOG = RC.CONSTRAINT_CATALOG  \r\n    AND KCU1.CONSTRAINT_SCHEMA = RC.CONSTRAINT_SCHEMA \r\n    AND KCU1.CONSTRAINT_NAME = RC.CONSTRAINT_NAME \r\n\r\nINNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KCU2 \r\n    ON KCU2.CONSTRAINT_CATALOG = RC.UNIQUE_CONSTRAINT_CATALOG  \r\n    AND KCU2.CONSTRAINT_SCHEMA = RC.UNIQUE_CONSTRAINT_SCHEMA \r\n    AND KCU2.CONSTRAINT_NAME = RC.UNIQUE_CONSTRAINT_NAME \r\n    AND KCU2.ORDINAL_POSITION = KCU1.ORDINAL_POSITION \r\n\r\nINNER JOIN (\r\n\tselect  TABLE_NAME,COLUMN_NAME, ORDINAL_POSITION from INFORMATION_SCHEMA.COLUMNS where IS_NULLABLE = 'NO' and DATA_TYPE in ('nvarchar','varchar') ) as TEXT_COLUMN \r\n\tON KCU2.TABLE_NAME = TEXT_COLUMN.TABLE_NAME\r\n\r\nORDER BY TABLE_NAME, COLUMN_NAME, TEXT_COLUMN.ORDINAL_POSITION";
        }

        private Object InvokeMethod(object instance, string methodName, Object[] args, out string message)
        {
            message = String.Empty;
            Type[] typeArray = new Type[args.Length];

            for (int I = 0; I < args.Length; I++)
                typeArray.SetValue(args[I].GetType(), I);

            MethodInfo mi = instance.GetType().GetMethod(methodName, typeArray);
            Object result;

            if (mi == null)
                throw new Exception($"Method --> [{methodName}] not supported by data provider --> {instance.GetType()}");

            try
            {
                result = mi.Invoke(instance, args);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    message = ex.InnerException.Message;
                else
                    message = ex.Message;
                return null;
            }

            return result;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Close()
        {
            CloseReader();

            if (Connection is IDbConnection)
            {
                if (Connection.State == ConnectionState.Open)
                {
                    Connection.Close();
                }
            }
        }

        ~DbNetDataCore()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && this.Provider != DataProvider.DataTable)
            {
                Close();
            }
        }
    }

    public class DbObject
    {
        public string QualifiedTableName { get; set; }
        public string TableName { get; set; }
    }
}
