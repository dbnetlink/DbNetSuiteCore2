using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Repositories;
using System.Data;
using System.Text;
using DbNetSuiteCore.Constants;
using DbNetSuiteCore.Extensions;

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
        protected readonly IOracleRepository _oracleRepository;

        protected HttpContext? _context = null;
        protected readonly IConfiguration _configuration;
        protected readonly IWebHostEnvironment _webHostEnvironment;

        public ComponentService(IMSSQLRepository msSqlRepository, RazorViewToStringRenderer razorRendererService, ISQLiteRepository sqliteRepository, IJSONRepository jsonRepository, IFileSystemRepository fileSystemRepository, IMySqlRepository mySqlRepository, IPostgreSqlRepository postgreSqlRepository, IExcelRepository excelRepository, IMongoDbRepository mongoDbRepository, IOracleRepository oracleRepository, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
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
            _oracleRepository = oracleRepository;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
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

                var primaryKeyAssigned = gridModel.Columns.Any(x => x.PrimaryKey);
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
                case DataSourceType.Oracle:
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
                    case DataSourceType.MySql:
                    case DataSourceType.PostgreSql:
                    case DataSourceType.SQLite:
                    case DataSourceType.Oracle:
                        componentModel.SetColumns(schema.Rows.Cast<DataRow>().Where(r => IsRowHidden(r) == false).Select(r => componentModel.NewColumn(r, componentModel.DataSourceType)).Where(c => c.Valid).ToList());
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
                    switch (componentModel.DataSourceType)
                    {
                        case DataSourceType.MSSQL:
                        case DataSourceType.MySql:
                        case DataSourceType.PostgreSql:
                        case DataSourceType.SQLite:
                        case DataSourceType.Oracle:
                            foreach (ColumnModel column in componentModel.GetColumns())
                            {
                                DataRow? dataRow = schema.Rows.Cast<DataRow>().FirstOrDefault(r => r["ColumnName"].ToString().ToLower() == column.Expression.ToLower());
                                if (dataRow != null)
                                {
                                    column.Update(dataRow, componentModel.DataSourceType);
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
                            break;
                        default:
                            for (var i = 0; i < dataColumns.Count; i++)
                            {
                                componentModel.GetColumns().ToList()[i].Update(dataColumns[i], componentModel.DataSourceType);
                            }
                            break;
                    }
                }
            }

            componentModel.SetColumns(componentModel.GetColumns().Where(c => c.Valid));

            for (var i = 0; i < componentModel.GetColumns().ToList().Count; i++)
            {
                componentModel.GetColumns().ToList()[i].Ordinal = i + 1;
            }

            ValidateModel(componentModel);
        }

        private bool IsRowHidden(DataRow dataRow)
        {
            if (string.IsNullOrEmpty((string)dataRow["ColumnName"]))
            {
                return true;
            }
            DataColumn? dataColumn = dataRow.Table.Columns["IsHidden"];
            if (dataColumn != null)
            {
                if (dataRow[dataColumn] != DBNull.Value)
                {
                    return (bool)dataRow["IsHidden"];
                }
            }
            return false;
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
                case DataSourceType.Oracle:
                    return await _oracleRepository.GetColumns(componentModel);
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
                case DataSourceType.Oracle:
                    await _oracleRepository.GetRecords(componentModel);
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
                case DataSourceType.Oracle:
                    await _oracleRepository.GetRecord(componentModel);
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
                case DataSourceType.Oracle:
                    return await _oracleRepository.RecordExists(componentModel, primaryKeyValue);
                case DataSourceType.Excel:
                case DataSourceType.MongoDB:
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
                case DataSourceType.Oracle:
                    await _oracleRepository.GetLookupOptions(componentModel);
                    break;
                case DataSourceType.MongoDB:
                    break;
                default:
                    await _msSqlRepository.GetLookupOptions(componentModel);
                    break;
            }
        }

        protected void AssignSearchDialogFilter(ComponentModel componentModel)
        {
            componentModel.SearchDialogFilter = new List<SearchDialogFilter>();
            var operatorList = RequestHelper.FormValueList("searchDialogOperator", _context).Select(f => f.Trim()).ToList();
            var value1List = RequestHelper.FormValueList("searchDialogValue1", _context).Select(f => f.Trim()).ToList();
            var value2List = RequestHelper.FormValueList("searchDialogValue2", _context).Select(f => f.Trim()).ToList();
            var keyList = RequestHelper.FormValueList("searchDialogKey", _context).Select(f => f.Trim()).ToList();

            for (var i = 0; i < operatorList.Count; i++)
            {
                if (string.IsNullOrEmpty(operatorList[i]))
                {
                    continue;
                }

                var searchDialogFilter = new SearchDialogFilter() { Operator = Enum.Parse<SearchOperator>(operatorList[i]), ColumnKey = keyList[i] };
                ColumnModel? columnModel = componentModel.GetColumns().FirstOrDefault(c => c.Key == searchDialogFilter.ColumnKey);

                if (columnModel == null)
                {
                    continue;
                }

                switch (searchDialogFilter.Operator)
                {
                    case SearchOperator.IsEmpty:
                    case SearchOperator.IsNotEmpty:
                    case SearchOperator.True:
                    case SearchOperator.False:
                        componentModel.SearchDialogFilter.Add(searchDialogFilter);
                        continue;
                }

                switch (searchDialogFilter.Operator)
                {
                    case SearchOperator.In:
                    case SearchOperator.NotIn:
                        /*
                        var paramList = new List<object?>();
                        foreach (var value in value1List[i].Split(','))
                        {
                            paramList.Add(ComponentModelExtensions.ParamValue(value, columnModel, componentModel.DataSourceType));
                        }
                        */
                        searchDialogFilter.Value1 = value1List[i];
                        break;
                    default:
                        searchDialogFilter.Value1 = value1List[i];// ComponentModelExtensions.ParamValue(value1List[i], columnModel, componentModel.DataSourceType);
                        break;
                }

                switch (searchDialogFilter.Operator)
                {
                    case SearchOperator.Between:
                    case SearchOperator.NotBetween:
                        if (string.IsNullOrEmpty(value2List[i]))
                        {
                            continue;
                        }
                        searchDialogFilter.Value2 = value2List[i];// ComponentModelExtensions.ParamValue(value2List[i], columnModel, componentModel.DataSourceType);
                        break;
                }

                componentModel.SearchDialogFilter.Add(searchDialogFilter);
            }
        }

        protected void CheckLicense(ComponentModel componentModel)
        {
            if (componentModel.Uninitialised)
            {
                componentModel.LicenseInfo = LicenseHelper.ValidateLicense(_configuration, _context, _webHostEnvironment);
            }
        }

        protected bool LengthError(ResourceNames resourceName, int? length, object? paramValue, GridFormColumn gridFormColumn, ComponentModel componentModel)
        {
            if (length.HasValue == false)
            {
                return false;
            }
            if ((resourceName == ResourceNames.MinCharsError && length.Value > gridFormColumn.ToStringOrEmpty(paramValue).Length) ||
                (resourceName == ResourceNames.MaxCharsError && length.Value < gridFormColumn.ToStringOrEmpty(paramValue).Length))
            {
                componentModel.Message = ResourceHelper.GetResourceString(resourceName).Replace("{0}", length.Value.ToString());
                gridFormColumn.InError = true;
                componentModel.MessageType = MessageType.Error;
                return true;
            }
            return false;
        }

        protected int Compare(object paramValue, object compareValue)
        {
            try
            {
                if (paramValue.GetType() != compareValue.GetType())
                {
                    compareValue = Convert.ChangeType(compareValue, paramValue.GetType());
                }
                string typeName = paramValue.GetType().Name;
                switch (typeName)
                {
                    case nameof(Int16):
                    case nameof(Int32):
                    case nameof(Int64):
                        return Comparer<Int64>.Default.Compare(Convert.ToInt64(paramValue), Convert.ToInt64(compareValue));
                    case nameof(Decimal):
                        return Comparer<Decimal>.Default.Compare(Convert.ToDecimal(paramValue), Convert.ToDecimal(compareValue));
                    case nameof(Single):
                    case nameof(Double):
                        return Comparer<Double>.Default.Compare(Convert.ToDouble(paramValue), Convert.ToDouble(compareValue));
                    case nameof(DateTime):
                        return Comparer<DateTime>.Default.Compare(Convert.ToDateTime(paramValue), Convert.ToDateTime(compareValue));
                }
            }
            catch (Exception)
            {
                return 0;
            }

            return 0;
        }
    }
}