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
using NUglify.Helpers;


namespace DbNetSuiteCore.Services
{
    public class FormService : ComponentService, IComponentService
    {
        public FormService(IMSSQLRepository msSqlRepository, RazorViewToStringRenderer razorRendererService, ISQLiteRepository sqliteRepository, IJSONRepository jsonRepository, IFileSystemRepository fileSystemRepository, IMySqlRepository mySqlRepository, IPostgreSqlRepository postgreSqlRepository, IExcelRepository excelRepository, IMongoDbRepository mongoDbRepository, IConfiguration configuration) : base(msSqlRepository, razorRendererService, sqliteRepository, jsonRepository, fileSystemRepository, mySqlRepository, postgreSqlRepository, excelRepository, mongoDbRepository, configuration)
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
                return await View("Error", ex);
            }
        }

        private async Task<Byte[]> FormView()
        {
            FormModel formModel = GetFormModel();
            formModel.TriggerName = RequestHelper.TriggerName(_context);

            ValidateModel(formModel);

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
                    string viewName = formModel.Uninitialised ? "Form/Markup" : "Form/Form";
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
                throw new Exception("At least one form column must be designated a primary key");
            }

            if (formModel.PrimaryKeyValues.Any() == false || formModel.TriggerName == TriggerNames.Search)
            {
                await GetRecords(formModel);
                formModel.PrimaryKeyValues = formModel.Data.AsEnumerable().Select(r => r.ItemArray[0]!).ToList();
                if (formModel.CurrentRecord > formModel.PrimaryKeyValues.Count)
                {
                    formModel.CurrentRecord = formModel.PrimaryKeyValues.Count;
                }
            }

            if (formModel.PrimaryKeyValues.Any())
            {
                var idx = Enumerable.Range(1, formModel.PrimaryKeyValues.Count()).Contains(formModel.CurrentRecord) ? formModel.CurrentRecord - 1 : 0;
                formModel.ParentKey = formModel.PrimaryKeyValues[idx]?.ToString() ?? string.Empty;
                formModel.FormValues.Clear();
                await GetRecord(formModel);
            }

            formModel.Mode = formModel.PrimaryKeyValues.Any() ? FormMode.Update : FormMode.Empty;
            var formViewModel = new FormViewModel(formModel);

            if (formModel.DiagnosticsMode)
            {
                formViewModel.Diagnostics = RequestHelper.Diagnostics(_context);
            }

            return formViewModel;
        }

        private async Task<Byte[]> ApplyUpdate(FormModel formModel)
        {
            FormViewModel formViewModel = new FormViewModel(formModel);
            if (ValidateRecord(formModel))
            {
                try
                {
                    if (formModel.Mode == FormMode.Update)
                    {
                        await UpdateRecord(formModel);
                        formModel.FormValues = new Dictionary<string, string>();
                    }
                    else
                    {
                        await InsertRecord(formModel);
                        formModel.PrimaryKeyValues = new List<object>();
                    }
                    formModel.Message = ResourceHelper.GetResourceString(ResourceNames.Updated);
                    formModel.MessageType = MessageType.Success;
                    
                }
                catch (Exception ex)
                {
                    formModel.Message = ex.Message;
                    formModel.MessageType = MessageType.Error;
                }
                formViewModel = await GetFormViewModel(formModel);
            }
            
            return await View("Form/Form", formViewModel);
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
            return await View("Form/Form", await GetFormViewModel(formModel));
        }

        private async Task<Byte[]> Toolbar(FormModel formModel)
        {
            return await View("Form/Toolbar", new FormViewModel(formModel));
        }

        private async Task<Byte[]> InitialiseInsert(FormModel formModel)
        {
            formModel.Mode = FormMode.Insert;
            await GetLookupOptions(formModel);
            return await View("Form/Form", new FormViewModel(formModel));
        }

        private bool ValidateRecord(FormModel formModel)
        {

            if (ValidateErrorType(formModel, ResourceNames.Required))
            {
                if (ValidateErrorType(formModel, ResourceNames.DataFormatError))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ValidateErrorType(FormModel formModel, ResourceNames resourceName)
        {
            foreach (string columnName in formModel.FormValues.Keys)
            {
                FormColumn? formColumn = formModel.Columns.First(c => c.ColumnName == columnName);

                if (formColumn == null)
                {
                    continue;
                }

                switch(resourceName)
                {
                    case ResourceNames.Required:
                        formColumn.InError = string.IsNullOrEmpty(formModel.FormValues[columnName]) && formColumn.Required;
                        break;  
                    case ResourceNames.DataFormatError:
                        object? paramValue = ComponentModelExtensions.ParamValue(formModel.FormValues[columnName], formColumn, formModel.DataSourceType);

                        if (paramValue == null)
                        {
                            formColumn.InError = true;
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


        private FormModel GetFormModel()
        {
            try
            {
                var model = TextHelper.DeobfuscateString(RequestHelper.FormValue("model", string.Empty, _context),_configuration);
                FormModel formModel = JsonConvert.DeserializeObject<FormModel>(model) ?? new FormModel();
                formModel.JSON = TextHelper.Decompress(RequestHelper.FormValue("json", string.Empty, _context));
                formModel.CurrentRecord = formModel.ToolbarPosition == ToolbarPosition.Hidden ? 1 : GetRecordNumber(formModel);
                formModel.SearchInput = RequestHelper.FormValue("searchInput", string.Empty, _context).Trim();
                formModel.FormValues = RequestHelper.FormColumnValues(_context);
                AssignParentKey(formModel);
                formModel.Columns.ForEach(column => column.InError = false);
                formModel.Message = string.Empty;
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
               //     await _mongoDbRepository.UpdateRecord(formModel);
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
                    //     await _mongoDbRepository.InsertRecord(formModel);
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
                    //     await _mongoDbRepository.DeleteRecord(formModel);
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