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
using Microsoft.Extensions.Options;
using DbNetSuiteCore.Middleware;
using DocumentFormat.OpenXml.Spreadsheet;
using DbNetSuiteCore.CustomisationHelpers.Interfaces;

namespace DbNetSuiteCore.Services
{
    public class FormService : ComponentService, IComponentService
    {
        public FormService(IMSSQLRepository msSqlRepository, RazorViewToStringRenderer razorRendererService, ISQLiteRepository sqliteRepository, IJSONRepository jsonRepository, IFileSystemRepository fileSystemRepository, IMySqlRepository mySqlRepository, IPostgreSqlRepository postgreSqlRepository, IExcelRepository excelRepository, IMongoDbRepository mongoDbRepository, IOracleRepository oracleRepository, IConfiguration configuration, IWebHostEnvironment webHostEnvironment, ILoggerFactory? loggerFactory) : base(msSqlRepository, razorRendererService, sqliteRepository, jsonRepository, fileSystemRepository, mySqlRepository, postgreSqlRepository, excelRepository, mongoDbRepository, oracleRepository, configuration, webHostEnvironment, loggerFactory)
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
                return await HandleError(ex, context);
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
                    var formViewModel = await GetFormViewModel(formModel); 
                    return await View(viewName, formViewModel);
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

            List<string> refreshTriggers = new List<string>() { TriggerNames.Search, TriggerNames.ParentKey, TriggerNames.SearchDialog };

            if (formModel.PrimaryKeyValues.Any() == false || refreshTriggers.Contains(formModel.TriggerName))
            {
                await GetRecords(formModel);
                formModel.PrimaryKeyValues = formModel.Data.AsEnumerable().Select(r => PrimaryKeyValue(r)).ToList();
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
                formViewModel.Diagnostics = RequestHelper.Diagnostics(_context, _configuration, _webHostEnvironment);
            }

            formModel.ValidationPassed = false;

            return formViewModel;
        }

        private List<object> PrimaryKeyValue(DataRow dataRow)
        {
            List<object> primaryKeyValues = new List<object>();
            foreach (object? value in (dataRow.ItemArray ?? new object[] { }))
            {
                if (value == null)
                {
                    continue;
                }
                if (value is ObjectId)
                {
                    primaryKeyValues.Add(((ObjectId)value).ToString());
                }
                else
                {
                    primaryKeyValues.Add(value);
                }
            }

            return primaryKeyValues;
        }
        private async Task<Byte[]> ApplyUpdate(FormModel formModel)
        {
            FormViewModel formViewModel = new FormViewModel(formModel);
            var committed = false;

            if (formModel.ClientEvents.Keys.Contains(FormClientEvent.ValidateUpdate) == false)
            {
                if (await ValidateRecord(formModel))
                {
                    await Commit();
                }

            }
            else if (formModel.ValidationPassed == false)
            {
                formModel.ValidationPassed = await ValidateRecord(formModel);
            }
            else
            {
                await Commit();
            }

            if (committed == false && formModel.Mode == FormMode.Update)
            {
                await GetRecord(formModel);
            }

            return await View("Form/__Form", formViewModel);

            async Task Commit()
            {
                await CommitUpdate(formModel);
                formViewModel = await GetFormViewModel(formModel);
                committed = true;
            }
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
                    formModel.PrimaryKeyValues = new List<List<object>>();
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
                if (CustomValidation(formModel, nameof(ICustomFormPlugin.ValidateDelete)))
                {
                    await DeleteRecord(formModel);
                    formModel.PrimaryKeyValues = new List<List<object>>();
                    formModel.Message = ResourceHelper.GetResourceString(ResourceNames.Deleted);
                    formModel.MessageType = MessageType.Success;
                }
                else
                {
                    throw new Exception(formModel.Message);
                }
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
            formModel.FormValues = new Dictionary<string, string>();
            await GetLookupOptions(formModel);
            return await View("Form/__Form", new FormViewModel(formModel));
        }

        private async Task<bool> ValidateRecord(FormModel formModel)
        {
            var validationTypes = new List<ResourceNames>() { ResourceNames.Required, ResourceNames.DataFormatError, ResourceNames.MinCharsError, ResourceNames.MaxCharsError, ResourceNames.MinValueError, ResourceNames.PatternError, ResourceNames.NotUnique };

            PopulateGuidPrimaryKey(formModel);

            foreach (var validationType in validationTypes)
            {
                if (ValidateErrorType(formModel, validationType) == false)
                {
                    return false;
                }
            }

            if (await ValidatePrimaryKey(formModel) == false)
            {
                return false;
            }

            return CustomValidation(formModel, formModel.Mode == FormMode.Update ? nameof(ICustomFormPlugin.ValidateUpdate): nameof(ICustomFormPlugin.ValidateInsert));
        }

        private void PopulateGuidPrimaryKey(FormModel formModel)
        {
            if (formModel.Mode == FormMode.Update)
            {
                return;
            }

            foreach (FormColumn? formColumn in formModel.Columns.Where(c => c.PrimaryKeyRequired && c.DataType == typeof(Guid)))
            {
                var columnName = formColumn.ColumnName;

                var value = string.Empty;

                if (formModel.FormValues.ContainsKey(columnName) == false || string.IsNullOrEmpty(formModel.FormValues[columnName]))
                {
                    formModel.FormValues[columnName] = Guid.NewGuid().ToString();
                }
            }
        }

        private bool CustomValidation(FormModel formModel, string methodName)
        {
            if (string.IsNullOrEmpty(formModel.CustomisationPluginName) || _context == null)
            {
                return true;
            }

            bool result = (bool)PluginHelper.InvokeMethod(formModel.CustomisationPluginName, methodName, new object[] { formModel, _context, _configuration })!;

            if (result == false)
            {
                if (string.IsNullOrEmpty(formModel.Message))
                {
                    formModel.Message = "Custom validation failed";
                }
                formModel.MessageType = MessageType.Error;
            }

            return result;
        }

        private async Task<bool> ValidatePrimaryKey(FormModel formModel)
        {
            if (formModel.Mode == FormMode.Update || formModel.Columns.Any(c => c.PrimaryKeyRequired) == false)
            {
                return true;
            }

            if (await PrimaryKeyExists(formModel))
            {
                formModel.Columns.Where(c => c.PrimaryKeyRequired).ToList().ForEach(c => c.InError = true);
                formModel.Message = ResourceHelper.GetResourceString(ResourceNames.PrimaryKeyExists);
                formModel.MessageType = MessageType.Error;
                return false;
            }

            return true;
        }

        private bool ValidateErrorType(FormModel formModel, ResourceNames resourceName)
        {
            foreach (FormColumn? formColumn in formModel.Columns.Where(c => c.DataOnly == false))
            {
                if (formModel.Mode == FormMode.Update && formColumn.PrimaryKey)
                {
                    continue;
                }

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

                ValidateFormValue(formColumn, value, resourceName, formModel);

                if (resourceName == ResourceNames.MinValueError && formModel.Columns.Any(c => c.InError))
                {
                    break;
                }
            }

            if (formModel.Columns.Any(c => c.InError))
            {
                if (string.IsNullOrEmpty(formModel.Message))
                {
                    formModel.Message = ResourceHelper.GetResourceString(resourceName);
                }
                formModel.MessageType = MessageType.Error;
                return false;
            }

            return true;
        }

        private FormModel GetFormModel()
        {
            try
            {
                FormModel formModel = JsonConvert.DeserializeObject<FormModel>(StateHelper.GetSerialisedModel(_context, _configuration)) ?? new FormModel();
                formModel.JSON = TextHelper.Decompress(RequestHelper.FormValue("json", string.Empty, _context));
                formModel.CurrentRecord = formModel.ToolbarPosition == ToolbarPosition.Hidden ? 1 : GetRecordNumber(formModel);
                formModel.SearchInput = RequestHelper.FormValue("searchInput", string.Empty, _context).Trim();
                formModel.FormValues = RequestHelper.FormColumnValues(_context, formModel);
                formModel.Columns.ToList().ForEach(column => column.InError = false);   
                formModel.Message = string.Empty;
                formModel.Modified = RequestHelper.GetModified(_context, formModel); ;
                formModel.ValidationPassed = ComponentModelExtensions.ParseBoolean(RequestHelper.FormValue("validationPassed", formModel.ValidationPassed.ToString(), _context));
                formModel.CommitType = null;
                formModel.SearchDialogConjunction = RequestHelper.FormValue("searchDialogConjunction", "and", _context).Trim();

                AssignParentModel(formModel);
                AssignSearchDialogFilter(formModel);

                return formModel;
            }
            catch (Exception)
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