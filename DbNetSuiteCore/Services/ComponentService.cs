using DbNetSuiteCore.Constants;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Factories.Interfaces;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using Newtonsoft.Json;
using System.Data;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace DbNetSuiteCore.Services
{
    public class ComponentService
    {
        protected readonly IRepositoryFactory _repositoryFactory;
        protected readonly RazorViewToStringRenderer _razorRendererService;

        protected HttpContext _context;
        protected readonly IConfiguration _configuration;
        protected readonly IWebHostEnvironment _webHostEnvironment;
        protected readonly ILoggerFactory _loggerFactory = null;
        protected readonly ILogger _logger = null;

        public ComponentService(
            IRepositoryFactory repositoryFactory,
            RazorViewToStringRenderer razorRendererService,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            ILoggerFactory loggerFactory)
        {
            _repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
            _razorRendererService = razorRendererService ?? throw new ArgumentNullException(nameof(razorRendererService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));

            if (loggerFactory != null)
            {
                _loggerFactory = loggerFactory;
                _logger = loggerFactory.CreateLogger(nameof(DbNetSuiteCore));
            }
        }

        protected async Task<Byte[]> HandleError(Exception ex, HttpContext context)
        {
            if (_logger != null)
            {
                _logger.LogError(ex, "Error processing DbNetSuiteCore control");
            }

            context.Response.StatusCode = 500;
            context.Response.Headers.Append("error", ex.Message.Normalize(NormalizationForm.FormKD).Where(x => x < 128).ToArray().ToString());
            return await View("__Error", ex);
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

                if (componentModel.DataSourceType == DataSourceType.FileSystem || DbHelper.IsInMemoryDb(componentModel.DataSourceType))
                {
                    gridModel.Columns.ToList().ForEach(c => c.Editable = false);
                }
            }

            switch (componentModel.DataSourceType)
            {
                case DataSourceType.SQLite:
                case DataSourceType.MSSQL:
                case DataSourceType.MySql:
                case DataSourceType.PostgreSql:
                case DataSourceType.Oracle:
                case DataSourceType.DuckDB:
                    if (string.IsNullOrEmpty(componentModel.ConnectionAlias) && componentModel.IsLinked == false)
                    {
                        throw new Exception($"The ConnectionAlias must be specified if the control is not linked to a parent control (<b>{componentModel.TableName}</b>)");
                    }
                    break;
            }


            switch (componentModel.DataSourceType)
            {
                case DataSourceType.FileSystem:
                    break;
                default:
                    if (componentModel.IsLinked && componentModel.GetColumns().Any(c => c.ForeignKey) == false && ((componentModel as FormModel)?.OneToOne ?? false == false) == false)
                    {
                        throw new Exception("A linked control must have a column designated as a <b>ForeignKey</b>");
                    }
                    break;
            }
        }

        protected void AssignParentModel(ComponentModel componentModel)
        {
            componentModel.HttpContext = _context;
            var primaryKey = RequestHelper.FormValue("primaryKey", string.Empty, _context);
            var foreignKey = RequestHelper.FormValue("foreignKey", string.Empty, _context);

            try
            {
                componentModel.AssignParentModel(_context, _configuration);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing page data: {ex.Message}", ex);
            }


            if (componentModel.DataSourceType == DataSourceType.FileSystem && componentModel.IsLinked)
            {
                componentModel.Url = componentModel.ParentModel.Url;// $"{componentModel.ParentModel.Url}/{componentModel.ParentModel.ParentRow[FileSystemColumn.Name.ToString()]}";
            }

            if (componentModel.IsLinked && componentModel is FormModel)
            {
                var formModel = (componentModel as FormModel)!;
                var foreignKeyColumn = formModel.Columns.FirstOrDefault(c => c.ForeignKey);
                if (foreignKeyColumn != null)
                {
                    switch (componentModel.TriggerName)
                    {
                        case TriggerNames.Apply:
                        case TriggerNames.Insert:
                            if (formModel.Mode == FormMode.Insert)
                            {
                                if (foreignKeyColumn.InitialValue != null)
                                {
                                    formModel.FormValues[foreignKeyColumn.ColumnName] = foreignKeyColumn.InitialValue?.ToString() ?? string.Empty;
                                }
                            }
                            break;
                        default:
                            if (string.IsNullOrEmpty(foreignKey) == false)
                            {
                                var foreignKeyValue = TextHelper.DeobfuscateKey<List<string>>(foreignKey ?? string.Empty);

                                if ((foreignKeyValue ?? new List<string>()).Any())
                                {
                                    foreignKeyColumn.InitialValue = foreignKeyValue!.First();
                                }
                            }
                            break;
                    }
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

                if (componentModel is FormModel formModel)
                {
                    formModel.Columns = formModel.Columns.Where(c => c.DataType != typeof(Byte[]));
                }

                ColumnsHelper.QualifyColumnExpressions(componentModel.GetColumns(), componentModel.DataSourceType);
            }
            else
            {
                var dataColumns = schema.Columns.Cast<DataColumn>().ToList();

                switch (componentModel.DataSourceType)
                {
                    case DataSourceType.FileSystem:
                        foreach (ColumnModel column in componentModel.GetColumns())
                        {
                            DataColumn dataColumn = dataColumns.FirstOrDefault(dc => dc.ColumnName.ToLower() == column.Expression.ToLower());
                            if (dataColumn != null)
                            {
                                column.Update(dataColumn, componentModel.DataSourceType);
                            }
                        }
                        break;
                    case DataSourceType.MSSQL:
                    case DataSourceType.MySql:
                    case DataSourceType.PostgreSql:
                    case DataSourceType.SQLite:
                    case DataSourceType.Oracle:
                    case DataSourceType.DuckDB:
                    case DataSourceType.JSON:
                        foreach (ColumnModel column in componentModel.GetColumns())
                        {
                            DataRow dataRow = schema.Rows.Cast<DataRow>().FirstOrDefault(r => (r["ColumnName"]?.ToString() ?? string.Empty).ToLower() == column.Expression.ToLower());
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

            componentModel.SetColumns(componentModel.GetColumns().Where(c => c.Valid));

            for (var i = 0; i < componentModel.GetColumns().ToList().Count; i++)
            {
                componentModel.GetColumns().ToList()[i].Ordinal = i + 1;
            }

            if (componentModel.DataSourceType == DataSourceType.FileSystem)
            {
                foreach (PropertyInfo property in typeof(Models.FileSystemInfo).GetProperties())
                {
                    var column = componentModel.GetColumns().FirstOrDefault(c => c.Name == property.Name);
                    if (column != null)
                    {
                        column.UserDataType = property.PropertyType.Name;

                        if (property.PropertyType == typeof(DateTime) && string.IsNullOrEmpty(column.Format))
                        {
                            column.Format = "f";
                        }
                    }
                }
            }

            ValidateModel(componentModel);
        }

        private bool IsRowHidden(DataRow dataRow)
        {
            if (string.IsNullOrEmpty((string)dataRow["ColumnName"]))
            {
                return true;
            }
            DataColumn dataColumn = dataRow.Table.Columns["IsHidden"];
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
            return await _repositoryFactory.GetSqlRepository(componentModel.DataSourceType).GetColumns(componentModel);
        }

        protected async Task GetRecords(ComponentModel componentModel)
        {
            await _repositoryFactory.GetSqlRepository(componentModel.DataSourceType).GetRecords(componentModel);
        }

        protected async Task<bool> PrimaryKeyExists(ComponentModel componentModel)
        {
            if (new[] {DataSourceType.JSON, DataSourceType.Excel, DataSourceType.FileSystem}.Contains(componentModel.DataSourceType))
            {
                return false;
            }

            return await _repositoryFactory.GetSqlRepository(componentModel.DataSourceType).PrimaryKeyExists(componentModel);
        }

        protected async Task<bool> ValueIsUnique(FormModel formModel, FormColumn formColumn)
        {
            switch (formModel.DataSourceType)
            {
                case DataSourceType.IEnumerable:
                case DataSourceType.FileSystem:
                    return false;
            }

            return await _repositoryFactory.GetSqlRepository(formModel.DataSourceType).ValueIsUnique(formModel, formColumn);
        }

        protected async Task GetRecord(ComponentModel componentModel)
        {
            await _repositoryFactory.GetSqlRepository(componentModel.DataSourceType).GetRecord(componentModel);
        }

        internal async Task<DataTable> GetRecordDataTable(ComponentModel componentModel)
        {
            // Only SQL data sources return DataTable
            return await _repositoryFactory.GetSqlRepository(componentModel.DataSourceType).GetRecordDataTable(componentModel);
        }

        protected async Task GetLookupOptions(ComponentModel componentModel)
        {
            // Only SQL data sources support lookup options
            await _repositoryFactory.GetSqlRepository(componentModel.DataSourceType).GetLookupOptions(componentModel);
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
                ColumnModel columnModel = componentModel.GetColumns().FirstOrDefault(c => c.Key == searchDialogFilter.ColumnKey);

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
                //   componentModel.LicenseInfo = LicenseHelper.ValidateLicense(_configuration, _context, _webHostEnvironment);
            }
        }

        protected void ValidateFormValue(FormColumn formColumn, string value, ResourceNames resourceName, ComponentModel componentModel)
        {
            object paramValue;

            switch (resourceName)
            {
                case ResourceNames.Required:
                    if (formColumn.DataType != typeof(Boolean))
                    {
                        bool primaryKeyRequired = (formColumn is FormColumn) ? ((FormColumn)formColumn).PrimaryKeyRequired : false;

                        if (formColumn.PrimaryKey && primaryKeyRequired == false)
                        {
                            break;
                        }
                        formColumn.InError = string.IsNullOrEmpty(value) && formColumn.Required;
                    }
                    break;
                case ResourceNames.DataFormatError:
                    paramValue = ComponentModelExtensions.ParamValue(value, formColumn, componentModel.DataSourceType);
                    if (paramValue == null)
                    {
                        formColumn.InError = true;
                    }
                    break;
                case ResourceNames.PatternError:
                    if (string.IsNullOrEmpty(value) == false && string.IsNullOrEmpty(formColumn.Pattern) == false)
                    {
                        if (new Regex(formColumn.Pattern).IsMatch(value) == false)
                        {
                            formColumn.InError = true;
                        }
                    }
                    break;
                case ResourceNames.MinCharsError:
                    if (formColumn.MinLength == null)
                    {
                        break;
                    }
                    paramValue = ComponentModelExtensions.ParamValue(value, formColumn, componentModel.DataSourceType);
                    if (paramValue == null)
                    {
                        break;
                    }
                    CheckForLengthError(ResourceNames.MinCharsError, formColumn.MinLength, paramValue, formColumn, componentModel);
                    break;
                case ResourceNames.MaxCharsError:
                    if (formColumn.MaxLength == null)
                    {
                        break;
                    }
                    paramValue = ComponentModelExtensions.ParamValue(value, formColumn, componentModel.DataSourceType);
                    if (paramValue == null)
                    {
                        break;
                    }
                    CheckForLengthError(ResourceNames.MaxCharsError, formColumn.MaxLength, paramValue, formColumn, componentModel);
                    break;
                case ResourceNames.MinValueError:
                    if (formColumn.MinValue == null && formColumn.MaxValue == null)
                    {
                        break;
                    }
                    paramValue = ComponentModelExtensions.ParamValue(value, formColumn, componentModel.DataSourceType);

                    bool lessThanMinimum = false;
                    bool greaterThanMaximum = false;

                    if (formColumn.MinValue != null)
                    {
                        lessThanMinimum = Compare(paramValue!, formColumn.MinValue) < 0;
                    }

                    if (formColumn.MaxValue != null)
                    {
                        greaterThanMaximum = Compare(paramValue!, formColumn.MaxValue) > 0;
                    }

                    if (lessThanMinimum)
                    {
                        componentModel.Message = string.Format(ResourceHelper.GetResourceString(ResourceNames.MinValueError), $"<b>{formColumn.Label}</b>&nbsp;", formColumn.MinValue);
                        formColumn.InError = true;
                    }

                    if (greaterThanMaximum)
                    {
                        formColumn.InError = true;
                        componentModel.Message = string.Format(ResourceHelper.GetResourceString(ResourceNames.MaxValueError), $"<b>{formColumn.Label}</b>&nbsp;", formColumn.MaxValue);
                    }
                    break;
                case ResourceNames.NotUnique:
                    if (formColumn.Unique)
                    {
                        paramValue = ComponentModelExtensions.ParamValue(value, formColumn, componentModel.DataSourceType);
                        if (paramValue == null)
                        {
                            break;
                        }
                        if (componentModel is FormModel formModel)
                        {
                            CheckForUniqueness(resourceName, paramValue, formColumn, formModel);
                        }
                    }
                    break;
            }
        }

        protected void CheckForLengthError(ResourceNames resourceName, int? length, object paramValue, FormColumn formColumn, ComponentModel componentModel)
        {
            if (length.HasValue)
            {
                if ((resourceName == ResourceNames.MinCharsError && length.Value > formColumn.ToStringOrEmpty(paramValue).Length) ||
                    (resourceName == ResourceNames.MaxCharsError && length.Value < formColumn.ToStringOrEmpty(paramValue).Length))
                {
                    componentModel.Message = ResourceHelper.GetResourceString(resourceName).Replace("{0}", length.Value.ToString());
                    formColumn.InError = true;
                    componentModel.MessageType = MessageType.Error;
                }
            }
        }

        protected void CheckForUniqueness(ResourceNames resourceName, object paramValue, FormColumn formColumn, FormModel formModel)
        {
            if (ValueIsUnique(formModel, formColumn).Result == false)
            {
                formModel.Message = ResourceHelper.GetResourceString(resourceName).Replace("{0}", $"&nbsp;<b>{formColumn.Label}</b>&nbsp;");
                formColumn.InError = true;
                formModel.MessageType = MessageType.Error;
            }
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

        protected void UpdateFixedFilterParameters(ComponentModel componentModel)
        {
            var fixedFilterParemeters = GetFormParameters(TriggerNames.FixedFilterParameters);
            foreach (DbParameter dbParameter in componentModel.FixedFilterParameters)
            {
                if (fixedFilterParemeters.ContainsKey(dbParameter.Name))
                {
                    dbParameter.Value = fixedFilterParemeters[dbParameter.Name];
                    componentModel.FixedFilterModified = true;
                }
            }
        }

        public void UpdateApiRequestParameters(ComponentModel componentModel)
        {
            var apiRequestParameters = GetFormParameters(TriggerNames.ApiRequestParameters);
            foreach (string key in componentModel.ApiRequestParameters.Keys)
            {
                if (apiRequestParameters.ContainsKey(key))
                {
                    componentModel.ApiRequestParameters[key] = HttpUtility.UrlEncode(apiRequestParameters[key]);
                }
            }
        }

        private Dictionary<string, string> GetFormParameters(string name)
        {
            Dictionary<string, string> apiRequestParameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(RequestHelper.FormValue(name, string.Empty, _context)) ?? new Dictionary<string, string>();

            return new Dictionary<string, string>(apiRequestParameters, StringComparer.OrdinalIgnoreCase);

        }
    }
}