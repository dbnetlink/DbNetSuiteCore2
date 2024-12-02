using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Repositories;
using System.Data;
using System.Text;
using DbNetSuiteCore.Constants;
using System.Text.RegularExpressions;

namespace DbNetSuiteCore.Services
{
    public class ComponentService
    {
        protected readonly IMSSQLRepository _msSqlRepository;
        protected readonly ISQLiteRepository _sqliteRepository;
        protected readonly RazorViewToStringRenderer _razorRendererService;
        protected readonly IJSONRepository _jsonRepository;
        protected readonly IFileSystemRepository _fileSystemRepository;
        protected readonly IMySqlRepository _mySqlRepository;
        protected readonly IPostgreSqlRepository _postgreSqlRepository;
        protected readonly IExcelRepository _excelRepository;
        protected readonly IMongoDbRepository _mongoDbRepository;
        protected HttpContext? _context = null;
        protected readonly IConfiguration _configuration;

        public ComponentService(IMSSQLRepository msSqlRepository, RazorViewToStringRenderer razorRendererService, ISQLiteRepository sqliteRepository, IJSONRepository jsonRepository, IFileSystemRepository fileSystemRepository, IMySqlRepository mySqlRepository, IPostgreSqlRepository postgreSqlRepository, IExcelRepository excelRepository, IMongoDbRepository mongoDbRepository, IConfiguration configuration)
        {
            _msSqlRepository = msSqlRepository;
            _razorRendererService = razorRendererService;
            _sqliteRepository = sqliteRepository;
            _jsonRepository = jsonRepository;
            _fileSystemRepository = fileSystemRepository;
            _mySqlRepository = mySqlRepository;
            _postgreSqlRepository = postgreSqlRepository;
            _excelRepository = excelRepository;
            _mongoDbRepository = mongoDbRepository;
            _configuration = configuration;
        }


        protected void ValidateModel(ComponentModel componentModel)
        {
            if (componentModel.TriggerName != TriggerNames.InitialLoad)
            {
                return;
            }

            if (componentModel is GridModel)
            {
                var gridModel = (GridModel)componentModel;

                var primaryKeyAssigned = gridModel.Columns.Any(x => x.PrimaryKey && x.Initialised);
                if (primaryKeyAssigned == false)
                {
                    if (gridModel.ViewDialog != null)
                    {
                        throw new Exception("A column designated as a <b>PrimaryKey</b> is required for the view dialog");
                    }

                    if (gridModel.GetLinkedControlIds().Any())
                    {
                        throw new Exception("A parent control must have a column designated as a <b>PrimaryKey</b>");
                    }
                }

                if (gridModel.GetLinkedControlIds().Any())
                {
                    if (gridModel.RowSelection != RowSelection.Single)
                    {
                        throw new Exception("A parent grid control must have <b>RowSelection</b> set to <b>Single</b>");
                    }
                }

            }

            switch (componentModel.DataSourceType)
            {
                case DataSourceType.MongoDB:
                case DataSourceType.SQLite:
                case DataSourceType.MSSQL:
                case DataSourceType.MySql:
                case DataSourceType.PostgreSql:
                    if (string.IsNullOrEmpty(componentModel.ConnectionAlias) && componentModel.IsLinked == false)
                    {
                        throw new Exception($"The ConnectionAlias must be specified if the control is not linked to a parent control (<b>{componentModel.TableName}</b>)");
                    }
                    break;
            }

            switch (componentModel.DataSourceType)
            {
                case DataSourceType.MongoDB:
                    if (string.IsNullOrEmpty(componentModel.DatabaseName))
                    {
                        throw new Exception("The DatabaseName property must also be supplied for MongoDB connections");
                    }
                    break;
            }

            switch (componentModel.DataSourceType)
            {
                case DataSourceType.FileSystem:
                    break;
                default:
                    if (componentModel.IsLinked && componentModel.GetColumns().Any(c => c.ForeignKey) == false)
                    {
                        throw new Exception("A linked control must have a column designated as a <b>ForeignKey</b>");
                    }
                    break;
            }
        }

        protected void AssignParentKey(ComponentModel componentModel)
        {
            var primaryKey = RequestHelper.FormValue("primaryKey", componentModel.ParentKey, _context);
            if (componentModel.DataSourceType == DataSourceType.FileSystem && componentModel.IsLinked)
            {
                componentModel.Url = primaryKey;
            }
            else
            {
                componentModel.ParentKey = primaryKey;
            }

            if (componentModel.IsLinked && componentModel is FormModel)
            {
                var foreignKeyColumn = (componentModel as FormModel).Columns.FirstOrDefault(c => c.ForeignKey);
                if (foreignKeyColumn != null)
                {
                    foreignKeyColumn.InitialValue = primaryKey;
                }
            }
        }

        protected async Task<Byte[]> View<TModel>(string viewName, TModel model)
        {
            return Encoding.UTF8.GetBytes(await _razorRendererService.RenderViewToStringAsync($"Views/{viewName}.cshtml", model));
        }

        protected async Task ConfigureColumns(ComponentModel componentModel)
        {
            componentModel.SetColumns(ColumnsHelper.MoveDataOnlyColumnsToEnd(componentModel.GetColumns()).ToList());

            DataTable schema = await GetColumns(componentModel);

            if (componentModel.GetColumns().Any() == false)
            {
                switch (componentModel.DataSourceType)
                {
                    case DataSourceType.MSSQL:
                        componentModel.SetColumns(schema.Rows.Cast<DataRow>().Where(r => (bool)r["IsHidden"] == false).Select(r => componentModel.NewColumn(r)).Where(c => c.Valid).ToList());
                        break;
                    default:
                        componentModel.SetColumns(schema.Columns.Cast<DataColumn>().Select(c => componentModel.NewColumn(c, componentModel.DataSourceType)).ToList());
                        break;
                }

                ColumnsHelper.QualifyColumnExpressions(componentModel.GetColumns(), componentModel.DataSourceType);
            }
            else
            {
                var dataColumns = schema.Columns.Cast<DataColumn>().ToList();

                if (componentModel.DataSourceType == DataSourceType.FileSystem)
                {
                    foreach (ColumnModel column in componentModel.GetColumns())
                    {
                        column.Update(dataColumns.First(dc => dc.ColumnName == column.Expression), componentModel.DataSourceType);
                    }
                }
                else
                {
                    if (componentModel.DataSourceType == DataSourceType.MSSQL/* && componentModel is FormModel*/)
                    {
                        foreach (ColumnModel column in componentModel.GetColumns())
                        {
                            DataRow? dataRow = schema.Rows.Cast<DataRow>().FirstOrDefault(r => r["ColumnName"].ToString() == column.Expression);
                            if (dataRow != null)
                            {
                                column.Update(dataRow);
                            }
                        }
                        if (componentModel.GetColumns().Any(c => string.IsNullOrEmpty(c.Name)))
                        {
                            componentModel.IgnoreSchemaTable = true;
                            schema = await GetColumns(componentModel);
                            dataColumns = schema.Columns.Cast<DataColumn>().ToList();
                            for (var i = 0; i < dataColumns.Count; i++)
                            {
                                componentModel.GetColumns().ToList()[i].Update(dataColumns[i], componentModel.DataSourceType);
                            }
                        }
                    }
                    else
                    {
                        for (var i = 0; i < dataColumns.Count; i++)
                        {
                            componentModel.GetColumns().ToList()[i].Update(dataColumns[i], componentModel.DataSourceType);
                        }
                    }
                }
            }

            if (componentModel is FormModel)
            {
                var formModel = (FormModel)componentModel;
                formModel.Columns = formModel.Columns.Where(c =>c.DataType != typeof(Byte[]));
            }

            for (var i = 0; i<componentModel.GetColumns().ToList().Count; i++)
            {
                componentModel.GetColumns().ToList()[i].Ordinal = i + 1;
            }
}

protected void ConfigureColumnsForStoredProcedure(ComponentModel componentModel)
{
    DataTable schema = componentModel.Data;

    if (componentModel.GetColumns().Any() == false)
    {
        componentModel.SetColumns(schema.Columns.Cast<DataColumn>().Select(dc => componentModel.NewColumn(dc, componentModel.DataSourceType)).ToList());
        ColumnsHelper.QualifyColumnExpressions(componentModel.GetColumns(), componentModel.DataSourceType);
    }
    else
    {
        var columns = componentModel.GetColumns();
        var dataColumns = schema.Columns.Cast<DataColumn>().ToList();

        for (var i = 0; i < dataColumns.Count; i++)
        {
            var dataColumn = dataColumns[i];
            var column = columns.FirstOrDefault(c => c.Expression.ToLower() == dataColumn.ColumnName.ToLower());

            if (column == null)
            {
                column = componentModel.NewColumn(dataColumn, componentModel.DataSourceType);
            }
            else
            {
                column.Update(dataColumn, componentModel.DataSourceType);
            }
            componentModel.SetColumns(componentModel.GetColumns().Append(column));
        }
    }
    for (var i = 0; i < componentModel.GetColumns().ToList().Count; i++)
    {
        componentModel.GetColumns().ToList()[i].Ordinal = i + 1;
    }
}

private async Task<DataTable> GetColumns(ComponentModel componentModel)
{
    switch (componentModel.DataSourceType)
    {
        case DataSourceType.SQLite:
            return await _sqliteRepository.GetColumns(componentModel);
        case DataSourceType.MySql:
            return await _mySqlRepository.GetColumns(componentModel);
        case DataSourceType.PostgreSql:
            return await _postgreSqlRepository.GetColumns(componentModel);
        case DataSourceType.JSON:
            return await _jsonRepository.GetColumns(componentModel, _context);
        case DataSourceType.Excel:
            return await _excelRepository.GetColumns(componentModel);
        case DataSourceType.FileSystem:
            return await _fileSystemRepository.GetColumns(componentModel, _context);
        case DataSourceType.MongoDB:
            return await _mongoDbRepository.GetColumns(componentModel);
        default:
            return await _msSqlRepository.GetColumns(componentModel);
    }
}

protected async Task GetRecords(ComponentModel componentModel)
{
    switch (componentModel.DataSourceType)
    {
        case DataSourceType.SQLite:
            await _sqliteRepository.GetRecords(componentModel);
            break;
        case DataSourceType.MySql:
            await _mySqlRepository.GetRecords(componentModel);
            break;
        case DataSourceType.PostgreSql:
            await _postgreSqlRepository.GetRecords(componentModel);
            break;
        case DataSourceType.JSON:
            await _jsonRepository.GetRecords(componentModel, _context);
            break;
        case DataSourceType.Excel:
            await _excelRepository.GetRecords(componentModel);
            break;
        case DataSourceType.FileSystem:
            await _fileSystemRepository.GetRecords(componentModel, _context);
            break;
        case DataSourceType.MongoDB:
            await _mongoDbRepository.GetRecords(componentModel);
            break;
        default:
            await _msSqlRepository.GetRecords(componentModel);
            break;
    }
}

protected async Task GetRecord(ComponentModel componentModel)
{
    switch (componentModel.DataSourceType)
    {
        case DataSourceType.SQLite:
            await _sqliteRepository.GetRecord(componentModel);
            break;
        case DataSourceType.MySql:
            await _mySqlRepository.GetRecord(componentModel);
            break;
        case DataSourceType.PostgreSql:
            await _postgreSqlRepository.GetRecord(componentModel);
            break;
        case DataSourceType.JSON:
            await _jsonRepository.GetRecord(componentModel, _context);
            break;
        case DataSourceType.Excel:
            await _excelRepository.GetRecord(componentModel);
            break;
        case DataSourceType.MongoDB:
            await _mongoDbRepository.GetRecord(componentModel);
            break;
        default:
            await _msSqlRepository.GetRecord(componentModel);
            break;
    }
}

protected async Task<bool> RecordExists(ComponentModel componentModel, object primaryKeyValue)
{
    switch (componentModel.DataSourceType)
    {
        case DataSourceType.SQLite:
            return await _sqliteRepository.RecordExists(componentModel, primaryKeyValue);
        case DataSourceType.MySql:
            return await _mySqlRepository.RecordExists(componentModel, primaryKeyValue);
        case DataSourceType.PostgreSql:
            return await _postgreSqlRepository.RecordExists(componentModel, primaryKeyValue);
        case DataSourceType.Excel:
            // await _excelRepository.RecordExists(componentModel, primaryKeyValue);
            break;
        case DataSourceType.MongoDB:
            // await _mongoDbRepository.RecordExists(componentModel, primaryKeyValue);
            break;
        default:
            return await _msSqlRepository.RecordExists(componentModel, primaryKeyValue);
    }

    return false;
}

protected async Task GetLookupOptions(ComponentModel componentModel)
{
    switch (componentModel.DataSourceType)
    {
        case DataSourceType.SQLite:
            await _sqliteRepository.GetLookupOptions(componentModel);
            break;
        case DataSourceType.MySql:
            await _mySqlRepository.GetLookupOptions(componentModel);
            break;
        case DataSourceType.PostgreSql:
            await _postgreSqlRepository.GetLookupOptions(componentModel);
            break;
        case DataSourceType.MongoDB:
            //   await _mongoDbRepository.GetLookupOptions(componentModel);
            break;
        default:
            await _msSqlRepository.GetLookupOptions(componentModel);
            break;
    }
}

    }
}