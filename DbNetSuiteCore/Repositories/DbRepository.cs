using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Extensions;
using System.Data;
using System.Data.Common;
using DbNetSuiteCore.Helpers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System.Linq;


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
            return DbHelper.GetConnection(database, _dataSourceType, _configuration, _env);
        }

        public async Task GetRecords(ComponentModel componentModel)
        {
            QueryCommandConfig query = componentModel.IsStoredProcedure ? componentModel.BuildProcedureCall() : componentModel.BuildQuery();
            componentModel.Data = await GetDataTable(query, componentModel.ConnectionAlias, (componentModel is FormModel ? null : componentModel), componentModel.IsStoredProcedure ? CommandType.StoredProcedure : CommandType.Text);

            if (componentModel is FormModel)
            {
                return;
            }

            if (componentModel is GridModel && ((GridModel)componentModel).PaginateQuery)
            {
                GridModel gridModel = (GridModel)componentModel;
                query = gridModel.BuildCountQuery();
                gridModel.TotalRows = Convert.ToInt32(await ExecuteScalar(query, componentModel));
            }

            if (componentModel.Data.Rows.Count > 0)
            {
                foreach (var column in componentModel.GetColumns().Where(c => c.LookupNotPopulated))
                {
                    await GetLookupOptions(componentModel, column);
                }

                ApplyLookups(componentModel);
            }

            await GetDbEnumOptions(componentModel);

            ComponentModelExtensions.ConvertEnumLookups(componentModel);

            if (componentModel is GridModel)
            {
                var gridModel = (GridModel)componentModel;
                if (gridModel.IsStoredProcedure == false)
                {
                    if (gridModel.SortColumn != null && gridModel.SortColumn.LookupOptions != null && gridModel.Data.Rows.Count > 0)
                    {
                        gridModel.Data = gridModel.Data.Select(string.Empty, gridModel.AddDataTableOrderPart()).CopyToDataTable();
                    }
                }
                else
                {
                    gridModel.Data.FilterAndSort(gridModel);
                }
            }
        }

        private void ApplyLookups(ComponentModel componentModel)
        {
            if (componentModel is GridModel)
            {
                foreach (var column in componentModel.GetColumns().Where(c => c.Lookup != null))
                {
                    DataColumn? dataColumn = componentModel.GetDataColumn(column);
                    componentModel.Data.ConvertLookupColumn(dataColumn, column, componentModel);
                }
            }
        }

        public async Task GetRecord(ComponentModel componentModel)
        {
            object? primaryKeyValue = null;
            if (componentModel is FormModel)
            {
                primaryKeyValue = ((FormModel)componentModel).RecordId;
            }
            QueryCommandConfig query = componentModel.BuildRecordQuery(primaryKeyValue);
            componentModel.Data = await GetDataTable(query, componentModel.ConnectionAlias, componentModel);
            await GetLookupOptions(componentModel);
            ApplyLookups(componentModel);
        }

        public async Task<bool> RecordExists(ComponentModel componentModel, object primaryKeyValue)
        {
            QueryCommandConfig query = componentModel.BuildRecordQuery(primaryKeyValue);
            var connection = GetConnection(componentModel.ConnectionAlias);
            connection.Open();
            var reader = await ExecuteQuery(query, connection);
            var recordExists = reader.HasRows;
            await reader.DisposeAsync();
            connection.Close();
            return recordExists;
        }

        public async Task GetLookupOptions(ComponentModel componentModel)
        {
            foreach (var column in componentModel.GetColumns().Where(c => c.LookupNotPopulated))
            {
                await GetLookupOptions(componentModel, column);
            }

            await GetDbEnumOptions(componentModel);

            if (componentModel is not FormModel)
            {
                componentModel.ConvertEnumLookups();
            }
        }

        public async Task GetDbEnumOptions(ComponentModel componentModel)
        {
            switch (componentModel.DataSourceType)
            {
                case DataSourceType.MySql:
                    foreach (var column in componentModel.GetColumns().Where(c => (c.DbDataType == MySqlDataTypes.Enum.ToString() || c.DbDataType == MySqlDataTypes.Set.ToString()) && c.LookupOptions == null))
                    {
                        List<string> options = await GetMySqlEnumOptions(componentModel.ConnectionAlias, column.BaseTableName, column.ColumnName);

                        column.DbLookupOptions = new List<KeyValuePair<string, string>>();

                        foreach (var option in options.OrderBy(o => o))
                        {
                            column.DbLookupOptions.Add(new KeyValuePair<string, string>(option, option));
                        }
                    }
                    break;
                case DataSourceType.PostgreSql:
                    foreach (var column in componentModel.GetColumns().Where(c => c.DbDataType == PostgreSqlDataTypes.Enum.ToString() && c.LookupOptions == null))
                    {
                        List<string> options = await GetPostgreSqlEnumOptions(componentModel, column.EnumName);

                        column.DbLookupOptions = new List<KeyValuePair<string, string>>();

                        foreach (var option in options.OrderBy(o => o))
                        {
                            column.DbLookupOptions.Add(new KeyValuePair<string, string>(option, option));
                        }
                    }
                    break;
            }
        }

        public async Task UpdateRecord(FormModel formModel)
        {
            CommandConfig update = formModel.BuildUpdate();
            var connection = GetConnection(formModel.ConnectionAlias);
            connection.Open();
            await ExecuteUpdate(update, connection);
            connection.Close();
        }

        public async Task InsertRecord(FormModel formModel)
        {
            switch(formModel.DataSourceType)
            {
                case DataSourceType.Oracle:
                    FormColumn? sequenceColumn = formModel.Columns.FirstOrDefault(c => string.IsNullOrEmpty(c.SequenceName) == false);

                    if (sequenceColumn != null)
                    {
                        formModel.FormValues[sequenceColumn.ColumnName] = (await GetOracleSequenceValue(sequenceColumn.SequenceName, formModel.ConnectionAlias)).ToString();
                    }
                    break;
            }

            CommandConfig update = formModel.BuildInsert();
            var connection = GetConnection(formModel.ConnectionAlias);
            connection.Open();
            await ExecuteUpdate(update, connection);
            connection.Close();
        }

        private async Task<long> GetOracleSequenceValue(string sequenceName, string connectionAliad)
        {
            Int64 sequenceValue = -1;
            using (IDbConnection connection = GetConnection(connectionAliad))
            {
                connection.Open();
                var reader = await ExecuteQuery($"SELECT {sequenceName}.nextval FROM DUAL", connection);
                if (reader.HasRows)
                {
                    reader.Read();
                    sequenceValue = Convert.ToInt64(reader[0]);
                }
                else
                {
                    throw new Exception($"Unable to read sequence => {sequenceName}");
                }
                connection.Close();
            }

            return sequenceValue;
        }

        public async Task DeleteRecord(FormModel formModel)
        {
            CommandConfig update = formModel.BuildDelete();
            var connection = GetConnection(formModel.ConnectionAlias);
            connection.Open();
            await ExecuteUpdate(update, connection);
            connection.Close();
        }

        private async Task GetLookupOptions(ComponentModel componentModel, ColumnModel column)
        {
            column.DbLookupOptions = new List<KeyValuePair<string, string>>();
            QueryCommandConfig query = new QueryCommandConfig(componentModel.DataSourceType);

            if (componentModel is GridModel)
            {
                GridModel gridModel = (GridModel)componentModel;

                if (gridModel.PaginateQuery)
                {
                    BuildLookupOptionsFromDbQuery(gridModel, column, ref query);
                }
                else
                {
                    BuildLookupOptionsFromDataTableQuery(gridModel, column, ref query);
                }

                if (string.IsNullOrEmpty(query.Sql))
                {
                    return;
                }
            }
            else
            {
                var lookup = column.Lookup!;

                if (string.IsNullOrEmpty(lookup.TableName))
                {
                    query.Sql = $"select distinct {column.ColumnName}, {column.ColumnName} from {componentModel.TableName} order by 1"; ;
                }
                else
                {
                    query.Sql = $"select {lookup.KeyColumn},{lookup.DescriptionColumn} from {lookup.TableName} order by 2";
                }
            }

            DataTable lookupData;

            try
            {
                lookupData = await GetDataTable(query, componentModel.ConnectionAlias);
            }
            catch (Exception ex)
            {
                throw new Exception("Error in column lookup configuration", ex);
            }

            column.DbLookupOptions = lookupData.AsEnumerable().Select(row => new KeyValuePair<string, string>(row[0]?.ToString() ?? string.Empty, row[1]?.ToString() ?? string.Empty)).ToList();
        }

        private void BuildLookupOptionsFromDataTableQuery(ComponentModel componentModel, ColumnModel column, ref QueryCommandConfig query)
        {
            var lookup = column.Lookup!;

            DataColumn? dataColumn = componentModel.GetDataColumn(column);

            if (dataColumn == null || componentModel.Data.Rows.Count == 0)
            {
                return;
            }
            var lookupValues = componentModel.Data.DefaultView.ToTable(true, dataColumn.ColumnName).Rows.Cast<DataRow>().Where(dr => dr[0] != DBNull.Value).Select(dr => Convert.ChangeType(dr[0], column.DataType)).OrderBy(v => v).ToList();

            if (string.IsNullOrEmpty(lookup.TableName))
            {
                column.DbLookupOptions = lookupValues.AsEnumerable().OrderBy(v => v).Select(v => new KeyValuePair<string, string>(v.ToString() ?? string.Empty, v.ToString() ?? string.Empty)).ToList();
                return;
            }

            var paramNames = Enumerable.Range(1, lookupValues.Count).Select(i => DbHelper.ParameterName($"param{i}", componentModel.DataSourceType)).ToList();

            int i = 0;
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            paramNames.ForEach(p => parameters[p] = lookupValues[i++]);
            query.Params = parameters;

            //var keyColumn = $"{lookup.KeyColumn}{(componentModel.DataSourceType == DataSourceType.PostgreSql ? "::varchar" : string.Empty)}";
            var keyColumn = $"{lookup.KeyColumn}";
            query.Sql = $"select {lookup.KeyColumn},{lookup.DescriptionColumn} from {lookup.TableName} where {keyColumn} in ({String.Join(",", paramNames)}) order by 2";
        }

        private void BuildLookupOptionsFromDbQuery(ComponentModel componentModel, ColumnModel column, ref QueryCommandConfig query)
        {
            var lookup = column.Lookup!;

            DataColumn? dataColumn = componentModel.GetDataColumn(column);

            if (dataColumn == null || componentModel.Data.Rows.Count == 0)
            {
                return;
            }
            var lookupValues = componentModel.Data.DefaultView.ToTable(true, dataColumn.ColumnName).Rows.Cast<DataRow>().Where(dr => dr[0] != DBNull.Value).Select(dr => Convert.ChangeType(dr[0], column.DataType)).OrderBy(v => v).ToList();

            if (string.IsNullOrEmpty(lookup.TableName))
            {
                query = componentModel.BuildDistinctQuery(column);
            }
            else
            {
                query = componentModel.BuildSubSelectQuery(column);
                query.Sql = $"select {lookup.KeyColumn},{lookup.DescriptionColumn} from {lookup.TableName} where {lookup.KeyColumn} in ({query.Sql}) order by 2";
            }
        }


        public async Task<List<object>> GetLookupKeys(GridModel gridModel, GridColumn gridColumn)
        {
            QueryCommandConfig query = new QueryCommandConfig(gridModel.DataSourceType);
            var lookup = gridColumn.Lookup!;

            var paramName = DbHelper.ParameterName(gridColumn.ParamName, gridModel.DataSourceType);
            query.Params[$"{paramName}"] = $"%{gridModel.SearchInput}%";
            query.Sql = $"select {lookup.KeyColumn} from {lookup.TableName} where {lookup.DescriptionColumn} like {paramName}";

            var dataTable = await GetDataTable(query, gridModel.ConnectionAlias);
            return dataTable.Rows.Cast<DataRow>().Select(dr => dr[0]).ToList();
        }

        public async Task<DataTable> GetColumns(ComponentModel componentModel)
        {
            QueryCommandConfig query = componentModel.BuildEmptyQuery();

            switch (componentModel.DataSourceType)
            {
                case DataSourceType.MSSQL:
                case DataSourceType.MySql:
                case DataSourceType.PostgreSql:
                case DataSourceType.SQLite:
                case DataSourceType.Oracle:
                    if (componentModel.IgnoreSchemaTable)
                    {
                        return await GetDataTable(query, componentModel.ConnectionAlias, null, CommandType.Text, CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo);
                    }
                    else
                    {
                        return await GetSchemaTable(query, componentModel.ConnectionAlias);
                    }
                default:
                    return await GetDataTable(query, componentModel.ConnectionAlias, null, CommandType.Text, CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo);
            }
        }

        public async Task<DataTable> GetDataTable(QueryCommandConfig queryCommandConfig, string database, ComponentModel? componentModel = null, CommandType commandType = CommandType.Text, CommandBehavior commandBehavior = CommandBehavior.Default)
        {
            using (IDbConnection connection = GetConnection(database))
            {
                connection.Open();
                DataTable dataTable = new DataTable();

                if (componentModel?.DataSourceType == DataSourceType.SQLite)
                {
                    if (componentModel.GetColumns().Any(c => c.AffinityDataType()))
                    {
                        ConfigureDataTableForSQLiteTypeAffinity(dataTable, componentModel);
                    }
                    try
                    {
                        DbDataReader dataReader = await ExecuteQuery(queryCommandConfig, connection, commandBehavior, commandType);
                        dataTable.Load(dataReader);
                        await dataReader.DisposeAsync();
                        connection.Close();
                        return dataTable;
                    }
                    catch (Exception)
                    {
                        dataTable = new DataTable();
                        /*
                        foreach (var column in componentModel.GetColumns())
                        {
                          //  dataTable.Columns.Add(column.ColumnName);
                        }
                        */
                    }
                }
                using (DataSet ds = new DataSet() { EnforceConstraints = false })
                {
                    ds.Tables.Add(dataTable);
                    DbDataReader dataReader = await ExecuteQuery(queryCommandConfig, connection, commandBehavior, commandType);
                    dataTable.Load(dataReader);
                    await dataReader.DisposeAsync();
                    ds.Tables.Remove(dataTable);
                }

                connection.Close();
                return dataTable;
            }
        }

        public void ConfigureDataTableForSQLiteTypeAffinity(DataTable dataTable, ComponentModel componentModel)
        {
            foreach (var column in componentModel.GetColumns())
            {
                switch (column.DataTypeName)
                {
                    case nameof(Decimal):
                    case nameof(Double):
                    case nameof(DateTime):
                    case "Byte[]":
                        dataTable.Columns.Add(column.ColumnName, column.DataType);
                        break;
                    default:
                        dataTable.Columns.Add(column.ColumnName);
                        break;
                }
            }
        }

        public async Task<DataTable> GetSchemaTable(QueryCommandConfig queryCommandConfig, string database)
        {
            using (IDbConnection connection = GetConnection(database))
            {
                connection.Open();
                var reader = await ExecuteQuery(queryCommandConfig, connection, CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo);
                DataTable dataTable = reader.GetSchemaTable() ?? new DataTable();
                await reader.DisposeAsync();
                connection.Close();
                return dataTable;
            }
        }

        public async Task<object?> ExecuteScalar(QueryCommandConfig query, ComponentModel componentModel)
        {
            using (IDbConnection connection = GetConnection(componentModel.ConnectionAlias))
            {
                connection.Open();
                IDbCommand command = DbHelper.ConfigureCommand(query, connection);
                return await ((DbCommand)command).ExecuteScalarAsync();
            }
        }

        public async Task<DbDataReader> ExecuteQuery(string query, IDbConnection connection)
        {
            IDbCommand command = DbHelper.ConfigureCommand(query, connection);
            return await ((DbCommand)command).ExecuteReaderAsync();
        }
        public async Task<DbDataReader> ExecuteQuery(QueryCommandConfig query, IDbConnection connection, CommandType commandType)
        {
            return await ExecuteQuery(query, connection, CommandBehavior.Default, commandType);
        }
        public async Task<DbDataReader> ExecuteQuery(QueryCommandConfig query, IDbConnection connection, CommandBehavior commandBehavour = CommandBehavior.Default, CommandType commandType = CommandType.Text)
        {
            IDbCommand command = DbHelper.ConfigureCommand(query, connection, commandType);
            return await ((DbCommand)command).ExecuteReaderAsync(commandBehavour);
        }

        public async Task ExecuteUpdate(CommandConfig update, IDbConnection connection)
        {
            if (_configuration.ConfigValue(ConfigurationHelper.AppSetting.UpdateDisabled).ToLower() == "true")
            {
                throw new Exception("Update has been disabled by configuration");
            }
            using (IDbCommand command = DbHelper.ConfigureCommand(update, connection, CommandType.Text))
            {
                await ((DbCommand)command).ExecuteNonQueryAsync();
            }
        }

        public async Task<List<string>> GetMySqlEnumOptions(string database, string tableName, string columnName)
        {
            var enumOptions = new List<string>();
            QueryCommandConfig query = new QueryCommandConfig(DataSourceType.MySql) { Sql = $"SHOW COLUMNS FROM {tableName} WHERE Field = '{columnName}'" };
            using (IDbConnection connection = GetConnection(database))
            {
                connection.Open();
                using (var reader = await ExecuteQuery(query, connection))
                {
                    if (reader.Read())
                    {
                        string columnType = reader["Type"].ToString();
                        // Extract options from column type definition
                        var matches = Regex.Matches(columnType, "'([^']*)'");

                        foreach (Match match in matches)
                        {
                            enumOptions.Add(match.Groups[1].Value);
                        }
                    }
                }
                connection.Close();
            }
            return enumOptions;
        }

        public async Task<List<string>> GetPostgreSqlEnumOptions(ComponentModel componentModel, string enumName)
        {
            QueryCommandConfig query = new QueryCommandConfig(componentModel.DataSourceType) { Sql = $"SELECT unnest(enum_range(NULL::{enumName}))" };
            var dataTable = await GetDataTable(query, componentModel.ConnectionAlias);
            var enumOptions = new List<string>();

            foreach (DataRow row in dataTable.Rows)
            {
                enumOptions.Add(row[0]?.ToString() ?? string.Empty);
            }
            return enumOptions;
        }

        public async Task<DataTable> GetColumnMetaData(QueryCommandConfig queryCommandConfig, string database)
        {
            using (IDbConnection connection = GetConnection(database))
            {
                connection.Open();

                DataTable datatable = new DataTable();
                datatable.Columns.Add("ColumnName", typeof(string));
                datatable.Columns.Add("DataType", typeof(Type));
                datatable.Columns.Add("DataTypeName", typeof(string));
                datatable.Columns.Add("ProviderType", typeof(Type));
                datatable.Columns.Add("ProviderTypeName", typeof(string));

                using (DbDataReader reader = await ExecuteQuery(queryCommandConfig, connection, CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo))
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var row = datatable.NewRow();
                        row["ColumnName"] = reader.GetName(i);
                        row["DataType"] = reader.GetFieldType(i);
                        row["DataTypeName"] = reader.GetDataTypeName(i);
                        row["ProviderType"] = reader.GetProviderSpecificFieldType(i);
                        row["ProviderTypeName"] = reader.GetProviderSpecificFieldType(i).Name;
                        datatable.Rows.Add(row);
                    }
                }

                return datatable;
            }
        }
    }
}