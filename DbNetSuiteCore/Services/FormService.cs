using DbNetSuiteCore.Services.Interfaces;
using DbNetSuiteCore.Repositories;
using System.Text;
using DbNetSuiteCore.Models;
using System.Data;
using DbNetSuiteCore.Helpers;
using Newtonsoft.Json;
using DbNetSuiteCore.ViewModels;
using DbNetSuiteCore.Constants;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Extensions;
using MongoDB.Bson;
using System.Text.RegularExpressions;

namespace DbNetSuiteCore.Services
{
    public class FormService : ComponentService, IComponentService
    {
        public FormService(IMSSQLRepository msSqlRepository, RazorViewToStringRenderer razorRendererService, ISQLiteRepository sqliteRepository, IJSONRepository jsonRepository, IFileSystemRepository fileSystemRepository, IMySqlRepository mySqlRepository, IPostgreSqlRepository postgreSqlRepository, IExcelRepository excelRepository, IMongoDbRepository mongoDbRepository, IOracleRepository oracleRepository, IConfiguration configuration, IWebHostEnvironment webHostEnvironment) : base(msSqlRepository, razorRendererService, sqliteRepository, jsonRepository, fileSystemRepository, mySqlRepository, postgreSqlRepository, excelRepository, mongoDbRepository, oracleRepository, configuration, webHostEnvironment)
        {
        }

        public async Task<Byte[]> Process(HttpContext context, string page)
        {
            try
            {
                _context = context;
                switch (page.ToLower())
                {
                    case "formcontrol":
                        return await FormView();
                    default:
                        return new byte[0];
                }
            }
            catch (Exception ex)
            {
                context.Response.Headers.Append("error", ex.Message.Normalize(NormalizationForm.FormKD).Where(x => x < 128).ToArray().ToString());
                return await View("__Error", ex);
            }
        }

        private async Task<Byte[]> FormView()
        {
            FormModel formModel = GetFormModel();
            formModel.TriggerName = RequestHelper.TriggerName(_context);

            CheckLicense(formModel);

            switch (formModel.TriggerName)
            {
                case TriggerNames.Apply:
                    return await ApplyUpdate(formModel);
                case TriggerNames.Toolbar:
                    return await Toolbar(formModel);
                case TriggerNames.Delete:
                    return await ApplyDelete(formModel);
                case TriggerNames.Insert:
                    return await InitialiseInsert(formModel);
                default:
                    string viewName = formModel.Uninitialised ? "Form/__Markup" : "Form/__Form";
                    return await View(viewName, await GetFormViewModel(formModel));
            }
        }

        private async Task<FormViewModel> GetFormViewModel(FormModel formModel)
        {
            if (formModel.Uninitialised)
            {
                await ConfigureColumns(formModel);
            }

            if (formModel.Columns.Any(c => c.PrimaryKey) == false)
            {
                throw new Exception("At least one form column must be designated as a primary key");
            }

            if (formModel.PrimaryKeyValues.Any() == false || formModel.TriggerName == TriggerNames.Search || formModel.TriggerName == TriggerNames.ParentKey || formModel.TriggerName == TriggerNames.SearchDialog)
            {
                await GetRecords(formModel);
                formModel.PrimaryKeyValues = formModel.Data.AsEnumerable().Select(r => PrimaryKeyValue(r.ItemArray[0])).ToList();
                if (formModel.CurrentRecord > formModel.PrimaryKeyValues.Count)
                {
                    formModel.CurrentRecord = formModel.PrimaryKeyValues.Count;
                }
            }

            formModel.Mode = FormMode.Empty;

            if (formModel.PrimaryKeyValues.Any())
            {
                formModel.Mode = FormMode.Update;
                formModel.CurrentRecord = Enumerable.Range(1, formModel.PrimaryKeyValues.Count()).Contains(formModel.CurrentRecord) ? formModel.CurrentRecord : 1;
                await GetRecord(formModel);
            }
            formModel.FormValues.Clear();

            var formViewModel = new FormViewModel(formModel);

            if (formModel.DiagnosticsMode)
            {
                formViewModel.Diagnostics = RequestHelper.Diagnostics(_context,_configuration,_webHostEnvironment);
            }

            formModel.ValidationPassed = false;

            return formViewModel;
        }

        private object PrimaryKeyValue(object value)
        {
            if (value is ObjectId)
            {
                value = ((ObjectId)value).ToString();
            }
            return value;
        }
        private async Task<Byte[]> ApplyUpdate(FormModel formModel)
        {
            FormViewModel formViewModel = new FormViewModel(formModel);
            var committed = false;

            if (formModel.ClientEvents.Keys.Contains(FormClientEvent.ValidateUpdate) == false)
            {
                if (await ValidateRecord(formModel))
                {
                    await CommitUpdate(formModel);
                    formViewModel = await GetFormViewModel(formModel);
                    committed = true;
                }

            }
            else if (formModel.ValidationPassed == false)
            {
                formModel.ValidationPassed = await ValidateRecord(formModel);
            }
            else
            {
                await CommitUpdate(formModel);
                formViewModel = await GetFormViewModel(formModel);
                committed = true;
            }

            if (committed == false && formModel.Mode == FormMode.Update)
            {
                await GetRecord(formModel);
            }

            return await View("Form/__Form", formViewModel);
        }

        private async Task CommitUpdate(FormModel formModel)
        {
            try
            {
                if (formModel.Mode == FormMode.Update)
                {
                    await UpdateRecord(formModel);
                    formModel.FormValues = new Dictionary<string, string>();
                    formModel.Message = ResourceHelper.GetResourceString(ResourceNames.Updated);
                }
                else
                {
                    await InsertRecord(formModel);
                    formModel.PrimaryKeyValues = new List<object>();
                    formModel.Message = ResourceHelper.GetResourceString(ResourceNames.Added);
                }

                formModel.MessageType = MessageType.Success;
                formModel.CommitType = formModel.Mode;

            }
            catch (Exception ex)
            {
                formModel.Message = ex.Message;
                formModel.MessageType = MessageType.Error;
            }
        }

        private async Task<Byte[]> ApplyDelete(FormModel formModel)
        {
            try
            {
                await DeleteRecord(formModel);
                formModel.PrimaryKeyValues = new List<object>();
                formModel.Message = ResourceHelper.GetResourceString(ResourceNames.Deleted);
                formModel.MessageType = MessageType.Success;
            }
            catch (Exception ex)
            {
                formModel.Message = ex.Message;
                formModel.MessageType = MessageType.Error;
            }
            return await View("Form/__Form", await GetFormViewModel(formModel));
        }

        private async Task<Byte[]> Toolbar(FormModel formModel)
        {
            return await View("Form/__Toolbar", new FormViewModel(formModel));
        }

        private async Task<Byte[]> InitialiseInsert(FormModel formModel)
        {
            formModel.Mode = FormMode.Insert;
            await GetLookupOptions(formModel);
            return await View("Form/__Form", new FormViewModel(formModel));
        }

        private async Task<bool> ValidateRecord(FormModel formModel)
        {
            if (ValidateErrorType(formModel, ResourceNames.Required))
            {
                if (ValidateErrorType(formModel, ResourceNames.DataFormatError))
                {
                    if (ValidateErrorType(formModel, ResourceNames.MinCharsError))
                    {
                        if (ValidateErrorType(formModel, ResourceNames.MinValueError))
                        {
                            if (ValidateErrorType(formModel, ResourceNames.PatternError))
                            {
                                if (await ValidatePrimaryKey(formModel))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        private async Task<bool> ValidatePrimaryKey(FormModel formModel)
        {
            if (formModel.Mode == FormMode.Update)
            {
                return true;
            }

            FormColumn? primaryKeyColumn = formModel.Columns.FirstOrDefault(c => c.PrimaryKeyRequired);
            if (primaryKeyColumn == null)
            {
                return true;
            }

            if (await RecordExists(formModel, formModel.FormValues[primaryKeyColumn.ColumnName]))
            {
                primaryKeyColumn.InError = true;
                formModel.Message = ResourceHelper.GetResourceString(ResourceNames.PrimaryKeyExists);
                formModel.MessageType = MessageType.Error;
                return false;
            }

            return true;
        }

        private bool ValidateErrorType(FormModel formModel, ResourceNames resourceName)
        {
            foreach (FormColumn? formColumn in formModel.Columns)
            {
                var columnName = formColumn.ColumnName;

                var value = string.Empty;

                if (formModel.FormValues.ContainsKey(columnName))
                {
                    value = formModel.FormValues[columnName];
                }
                else if (formModel.Mode == FormMode.Insert && formColumn.PrimaryKeyRequired)
                {
                    value = string.Empty;
                }
                else if (formModel.Mode == FormMode.Update || formColumn.Required == false || formColumn.PrimaryKey)
                {
                    continue;
                }

                object? paramValue;

                switch (resourceName)
                {
                    case ResourceNames.Required:
                        formColumn.InError = string.IsNullOrEmpty(value) && (formColumn.Required || formColumn.PrimaryKeyRequired);
                        break;
                    case ResourceNames.DataFormatError:
                        paramValue = ComponentModelExtensions.ParamValue(value, formColumn, formModel.DataSourceType);
                        if (paramValue == null)
                        {
                            formColumn.InError = true;
                        }
                        break;
                    case ResourceNames.PatternError:
                        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(formColumn.Pattern))
                        {
                            break;
                        }
                        if (new Regex(formColumn.Pattern).IsMatch(value) == false)
                        {
                            formColumn.InError = true;
                        }
                        break;
                    case ResourceNames.MinCharsError:
                        if (formColumn.MinLength == null && formColumn.MaxLength == null)
                        {
                            continue;
                        }
                        paramValue = ComponentModelExtensions.ParamValue(value, formColumn, formModel.DataSourceType);
                        if (paramValue == null)
                        {
                            continue;
                        }

                        if (LengthError(ResourceNames.MinCharsError, formColumn.MinLength, paramValue, formColumn,formModel))
                        {
                            return false;
                        }
                        if (LengthError(ResourceNames.MaxCharsError, formColumn.MaxLength, paramValue, formColumn, formModel))
                        {
                            return false;
                        }

                        break;
                    case ResourceNames.MinValueError:
                        if (formColumn.MinValue == null && formColumn.MaxValue == null)
                        {
                            continue;
                        }
                        paramValue = ComponentModelExtensions.ParamValue(value, formColumn, formModel.DataSourceType);

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
                            formModel.Message = string.Format(ResourceHelper.GetResourceString(ResourceNames.MinValueError), $"<b>{formColumn.Label}</b>", formColumn.MinValue);
                        };

                        if (greaterThanMaximum)
                        {
                            formModel.Message = string.Format(ResourceHelper.GetResourceString(ResourceNames.MaxValueError), $"<b>{formColumn.Label}</b>", formColumn.MaxValue);
                        };

                        if (string.IsNullOrEmpty(formModel.Message) == false)
                        {
                            formModel.MessageType = MessageType.Error;
                            return false;
                        }
                        break;
                }
            }

            if (formModel.Columns.Any(c => c.InError))
            {
                formModel.Message = ResourceHelper.GetResourceString(resourceName);
                formModel.MessageType = MessageType.Error;
                return false;
            }

            return true;
        }

        private bool LengthError(ResourceNames resourceName, int? length, object? paramValue, FormColumn formColumn, FormModel formModel)
        {
            if (length.HasValue == false)
            { 
                return false;
            }
            if ((resourceName == ResourceNames.MinCharsError && length.Value > formColumn.ToStringOrEmpty(paramValue).Length) || 
                (resourceName == ResourceNames.MaxCharsError && length.Value < formColumn.ToStringOrEmpty(paramValue).Length))
            {
                formModel.Message = ResourceHelper.GetResourceString(resourceName).Replace("{0}", length.Value.ToString());
                formColumn.InError = true;
                formModel.MessageType = MessageType.Error;
                return true;
            }
            return false;
        }

        private int Compare(object paramValue, object compareValue)
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

        private FormModel GetFormModel()
        {
            try
            {
                var model = TextHelper.DeobfuscateString(RequestHelper.FormValue("model", string.Empty, _context), _configuration);
                FormModel formModel = JsonConvert.DeserializeObject<FormModel>(model) ?? new FormModel();
                formModel.JSON = TextHelper.Decompress(RequestHelper.FormValue("json", string.Empty, _context));
                formModel.CurrentRecord = formModel.ToolbarPosition == ToolbarPosition.Hidden ? 1 : GetRecordNumber(formModel);
                formModel.SearchInput = RequestHelper.FormValue("searchInput", string.Empty, _context).Trim();
                formModel.FormValues = RequestHelper.FormColumnValues(_context);
                formModel.Columns.ToList().ForEach(column => column.InError = false);
                formModel.Message = string.Empty;
                formModel.ValidationPassed = ComponentModelExtensions.ParseBoolean(RequestHelper.FormValue("validationPassed", formModel.ValidationPassed.ToString(), _context));
                formModel.CommitType = null;
                formModel.SearchDialogConjunction = RequestHelper.FormValue("searchDialogConjunction", "and", _context).Trim();

                AssignParentKey(formModel);
                AssignSearchDialogFilter(formModel);

                return formModel;
            }
            catch
            {
                return new FormModel();
            }
        }

        protected async Task UpdateRecord(FormModel formModel)
        {
            switch (formModel.DataSourceType)
            {
                case DataSourceType.SQLite:
                    await _sqliteRepository.UpdateRecord(formModel);
                    break;
                case DataSourceType.MySql:
                    await _mySqlRepository.UpdateRecord(formModel);
                    break;
                case DataSourceType.PostgreSql:
                    await _postgreSqlRepository.UpdateRecord(formModel);
                    break;
                case DataSourceType.MongoDB:
                    await _mongoDbRepository.UpdateRecord(formModel);
                    break;
                case DataSourceType.Oracle:
                    await _oracleRepository.UpdateRecord(formModel);
                    break;
                default:
                    await _msSqlRepository.UpdateRecord(formModel);
                    break;
            }
        }

        protected async Task InsertRecord(FormModel formModel)
        {
            switch (formModel.DataSourceType)
            {
                case DataSourceType.SQLite:
                    await _sqliteRepository.InsertRecord(formModel);
                    break;
                case DataSourceType.MySql:
                    await _mySqlRepository.InsertRecord(formModel);
                    break;
                case DataSourceType.PostgreSql:
                    await _postgreSqlRepository.InsertRecord(formModel);
                    break;
                case DataSourceType.MongoDB:
                    await _mongoDbRepository.InsertRecord(formModel);
                    break;
                case DataSourceType.Oracle:
                    await _oracleRepository.InsertRecord(formModel);
                    break;
                default:
                    await _msSqlRepository.InsertRecord(formModel);
                    break;
            }
        }

        protected async Task DeleteRecord(FormModel formModel)
        {
            switch (formModel.DataSourceType)
            {
                case DataSourceType.SQLite:
                    await _sqliteRepository.DeleteRecord(formModel);
                    break;
                case DataSourceType.MySql:
                    await _mySqlRepository.DeleteRecord(formModel);
                    break;
                case DataSourceType.PostgreSql:
                    await _postgreSqlRepository.DeleteRecord(formModel);
                    break;
                case DataSourceType.MongoDB:
                    await _mongoDbRepository.DeleteRecord(formModel);
                    break;
                case DataSourceType.Oracle:
                    await _oracleRepository.DeleteRecord(formModel);
                    break;
                default:
                    await _msSqlRepository.DeleteRecord(formModel);
                    break;
            }
        }

        private int GetRecordNumber(FormModel formModel)
        {
            switch (RequestHelper.TriggerName(_context))
            {
                case TriggerNames.Record:
                    return Convert.ToInt32(RequestHelper.FormValue(TriggerNames.Record, "1", _context));
                case TriggerNames.Search:
                case TriggerNames.First:
                case TriggerNames.ParentKey:
                    return 1;
                case TriggerNames.Next:
                    return formModel.CurrentRecord + 1;
                case TriggerNames.Previous:
                    return formModel.CurrentRecord - 1;
                case TriggerNames.Last:
                    return formModel.PrimaryKeyValues.Count();
            }

            return formModel.CurrentRecord;
        }

        private bool IsNavigationRequest()
        {
            switch (RequestHelper.TriggerName(_context))
            {
                case TriggerNames.Record:
                case TriggerNames.First:
                case TriggerNames.Next:
                case TriggerNames.Previous:
                case TriggerNames.Last:
                    return true;
            }

            return false;
        }
    }
}