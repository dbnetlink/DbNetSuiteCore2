using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Extensions;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using DbNetSuiteCore.Helpers;

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
            componentModel.Data = await GetDataTable(query, componentModel.ConnectionAlias, componentModel.IsStoredProcedure);

            
            if (componentModel.Data.Rows.Count > 0)
            {
                foreach (var column in componentModel.GetColumns().Where(c => c.Lookup != null && c.LookupOptions == null))
                {
                    await GetLookupOptions(componentModel, column);
                }
            }

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

        public async Task GetRecord(GridModel gridModel)
        {
            QueryCommandConfig query = gridModel.BuildRecordQuery();
            gridModel.Data = await GetDataTable(query, gridModel.ConnectionAlias);

            foreach (var gridColumn in gridModel.Columns.Where(c => c.Lookup != null && c.LookupOptions == null))
            {
                await GetLookupOptions(gridModel, gridColumn);
            }

            gridModel.ConvertEnumLookups();
        }

        private async Task GetLookupOptions(ComponentModel gridModel, ColumnModel gridColumn)
        {
            DataColumn? dataColumn = gridModel.GetDataColumn(gridColumn);

            gridColumn.DbLookupOptions = new List<KeyValuePair<string, string>>();

            if (dataColumn == null || gridModel.Data.Rows.Count == 0)
            {
                return;
            }

            var lookupValues = gridModel.Data.DefaultView.ToTable(true, dataColumn.ColumnName).Rows.Cast<DataRow>().Select(dr => dr[0].ToString()).ToList();

            var lookup = gridColumn.Lookup!;

            if (string.IsNullOrEmpty(lookup.TableName))
            {
                gridColumn.DbLookupOptions = lookupValues.AsEnumerable().OrderBy(v => v).Select(v => new KeyValuePair<string, string>(v.ToString() ?? string.Empty, v.ToString() ?? string.Empty)).ToList();
                return;
            }

            QueryCommandConfig query = new QueryCommandConfig();
            var paramNames = Enumerable.Range(1, lookupValues.Count).Select(i => DbHelper.ParameterName($"param{i}")).ToList();

            int i = 0;
            paramNames.ForEach(p => query.Params[p] = lookupValues[i++]);

            var keyColumn = $"{lookup.KeyColumn}{(gridModel.DataSourceType == DataSourceType.PostgreSql ? "::varchar" : string.Empty)}";

            query.Sql = $"select {lookup.KeyColumn},{lookup.DescriptionColumn} from {lookup.TableName} where {keyColumn} in ({String.Join(",", paramNames)}) order by 2";

            DataTable lookupData;

            try
            {
                lookupData = await GetDataTable(query, gridModel.ConnectionAlias);
            }
            catch (Exception ex)
            {
                throw new Exception("Error in column lookup configuration", ex);
            }

            gridColumn.DbLookupOptions = lookupData.AsEnumerable().Select(row => new KeyValuePair<string, string>(row[0]?.ToString() ?? string.Empty, row[1]?.ToString() ?? string.Empty)).ToList();
            gridModel.Data.ConvertLookupColumn(dataColumn, gridColumn, gridModel);
        }

        public async Task<List<object>> GetLookupKeys(GridModel gridModel, GridColumn gridColumn)
        {
            QueryCommandConfig query = new QueryCommandConfig();
            var lookup = gridColumn.Lookup!;

            query.Params[$"@{gridColumn.ParamName}"] = $"%{gridModel.SearchInput}%";
            query.Sql = $"select {lookup.KeyColumn} from {lookup.TableName} where {lookup.DescriptionColumn} like @{gridColumn.ParamName}";

            var dataTable = await GetDataTable(query, gridModel.ConnectionAlias);
            return dataTable.Rows.Cast<DataRow>().Select(dr => dr[0]).ToList();
        }

        public async Task<DataTable> GetColumns(ComponentModel componentModel)
        {
            QueryCommandConfig query = componentModel.BuildEmptyQuery();

            switch (componentModel.DataSourceType)
            {
                case DataSourceType.MSSQL:
                    if (componentModel.GetColumns().Any())
                    {
                        return await GetDataTable(query, componentModel.ConnectionAlias);
                    }
                    else
                    {
                        return await GetSchemaTable(query, componentModel.ConnectionAlias);
                    }
                default:
                    return await GetDataTable(query, componentModel.ConnectionAlias);
            }
        }

        public async Task<DataTable> GetDataTable(QueryCommandConfig queryCommandConfig, string database, bool isStoredProcedure = false)
        {
            var connection = GetConnection(database);
            connection.Open();
            DataTable dataTable = new DataTable();
            dataTable.Load(await ExecuteQuery(queryCommandConfig, connection, isStoredProcedure ? CommandType.StoredProcedure : CommandType.Text));
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
        public async Task<DbDataReader> ExecuteQuery(QueryCommandConfig query, IDbConnection connection, CommandType commandType)
        {
            return await ExecuteQuery(query, connection, CommandBehavior.Default, commandType);
        }
        public async Task<DbDataReader> ExecuteQuery(QueryCommandConfig query, IDbConnection connection, CommandBehavior commandBehavour = CommandBehavior.Default, CommandType commandType = CommandType.Text)
        {
            IDbCommand command = DbHelper.ConfigureCommand(query.Sql, connection, query.Params, commandType);
            return await ((DbCommand)command).ExecuteReaderAsync(commandBehavour);
        }

        public async Task<int> ExecuteNonQuery(CommandConfig commandConfig, IDbConnection connection)
        {
            if (Regex.Match(commandConfig.Sql, "^(delete|update) ", RegexOptions.IgnoreCase).Success)
                if (!Regex.Match(commandConfig.Sql, " where ", RegexOptions.IgnoreCase).Success)
                    throw new Exception("Unqualified updates and deletes are not allowed.");

            IDbCommand command = DbHelper.ConfigureCommand(commandConfig.Sql, connection, commandConfig.Params);
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
    }
}