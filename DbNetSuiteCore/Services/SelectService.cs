using DbNetSuiteCore.Constants;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Middleware;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Services.Interfaces;
using DbNetSuiteCore.ViewModels;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Data;
using System.Text;

namespace DbNetSuiteCore.Services
{
    public class SelectService : ComponentService, IComponentService
    {
        public SelectService(IMSSQLRepository msSqlRepository, RazorViewToStringRenderer razorRendererService, ISQLiteRepository sqliteRepository, IJSONRepository jsonRepository, IFileSystemRepository fileSystemRepository, IMySqlRepository mySqlRepository, IPostgreSqlRepository postgreSqlRepository, IExcelRepository excelRepository, IMongoDbRepository mongoDbRepository, IOracleRepository oracleRepository, IConfiguration configuration, IWebHostEnvironment webHostEnvironment, ILoggerFactory loggerFactory) : base(msSqlRepository, razorRendererService, sqliteRepository, jsonRepository, fileSystemRepository, mySqlRepository, postgreSqlRepository, excelRepository, mongoDbRepository, oracleRepository, configuration, webHostEnvironment, loggerFactory)
        {
        }

    public async Task<Byte[]> Process(HttpContext context, string page)
    {
            try
            {
                _context = context;
                switch (page.ToLower())
                {
                    case "selectcontrol":
                        return await SelectView();
                    default:
                        return new byte[0];
                }
            }
            catch (Exception ex)
            {   
                return await HandleError(ex, context);
            }
        }

        private async Task<Byte[]> SelectView()
        {
            SelectModel selectModel = GetSelectModel() ?? new SelectModel();
            selectModel.TriggerName = _context == null ? string.Empty : RequestHelper.TriggerName(_context);

            string viewName = selectModel.TriggerName == TriggerNames.InitialLoad ? "Select/__Markup" : "Select/__Options";
            return await View(viewName, await GetSelectViewModel(selectModel));
        }

        private async Task<SelectViewModel> GetSelectViewModel(SelectModel selectModel)
        {
            if (selectModel.DataSourceType == DataSourceType.FileSystem && string.IsNullOrEmpty(selectModel.ParentModel?.Name) == false)
            {
                FileSystemRepository.UpdateUrl(selectModel);
            }

            if (String.IsNullOrEmpty(selectModel.DataSourcePluginName) == false)
            {
                try
                {
                    PluginHelper.InvokeMethod(selectModel.DataSourcePluginName, nameof(IDataSourcePlugin.GetData), selectModel, null, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error invoking plugin {nameof(IDataSourcePlugin)} => {nameof(IDataSourcePlugin.GetData)}");
                    throw;
                }
            }
            else
            {
                if (selectModel.IsStoredProcedure == false && selectModel.Uninitialised)
                {
                    await ConfigureColumns(selectModel);
                }
                await GetRecords(selectModel);
                if (selectModel.IsStoredProcedure && selectModel.Uninitialised)
                {
                    ConfigureColumnsForStoredProcedure(selectModel);
                }
            }

            if (selectModel.DataSourceType == DataSourceType.FileSystem)
            {
                selectModel.SummaryModel = new SummaryModel(selectModel);
            }

            var selectViewModel = new SelectViewModel(selectModel);

            if (selectModel.DiagnosticsMode)
            {
                selectViewModel.Diagnostics = RequestHelper.Diagnostics(_context, _configuration, _webHostEnvironment);
            }

            //selectModel.SummaryModel = new SummaryModel(selectModel);

            return selectViewModel;
        }
 
        private SelectModel GetSelectModel()
        {
            try
            {
                SelectModel selectModel = JsonConvert.DeserializeObject<SelectModel>(StateHelper.GetSerialisedModel(_context, _configuration)) ?? new SelectModel();
                selectModel.JSON = TextHelper.Decompress(RequestHelper.FormValue("json", string.Empty, _context));
                AssignParentModel(selectModel);
                selectModel.SearchInput = RequestHelper.FormValue("searchInput", string.Empty, _context).Trim(); 
  
                UpdateApiRequestParameters(selectModel);
                UpdateFixedFilterParameters(selectModel);
                return selectModel;
            }
            catch(Exception)
            {
                return new SelectModel();
            }
        }
    }
}