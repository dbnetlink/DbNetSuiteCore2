using DbNetSuiteCore.Services.Interfaces;
using DbNetSuiteCore.Repositories;
using System.Text;
using DbNetSuiteCore.Models;
using System.Data;
using DbNetSuiteCore.Helpers;
using Newtonsoft.Json;
using DbNetSuiteCore.ViewModels;
using Microsoft.Extensions.Options;
using DbNetSuiteCore.Middleware;
using DbNetSuiteCore.Enums;

namespace DbNetSuiteCore.Services
{
    public class SelectService : ComponentService, IComponentService
    {
        public SelectService(IMSSQLRepository msSqlRepository, RazorViewToStringRenderer razorRendererService, ISQLiteRepository sqliteRepository, IJSONRepository jsonRepository, IFileSystemRepository fileSystemRepository, IMySqlRepository mySqlRepository, IPostgreSqlRepository postgreSqlRepository, IExcelRepository excelRepository, IMongoDbRepository mongoDbRepository, IOracleRepository oracleRepository, IConfiguration configuration, IWebHostEnvironment webHostEnvironment) : base(msSqlRepository, razorRendererService, sqliteRepository, jsonRepository, fileSystemRepository, mySqlRepository, postgreSqlRepository, excelRepository, mongoDbRepository, oracleRepository, configuration, webHostEnvironment)
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
                context.Response.Headers.Append("error", ex.Message.Normalize(NormalizationForm.FormKD).Where(x => x < 128).ToArray().ToString());
                return await View("__Error", ex);
            }
        }

        private async Task<Byte[]> SelectView()
        {
            SelectModel selectModel = GetSelectModel() ?? new SelectModel();
            selectModel.TriggerName = _context == null ? string.Empty : RequestHelper.TriggerName(_context);

            string viewName = selectModel.Uninitialised ? "Select/__Markup" : "Select/__Options";
            return await View(viewName, await GetSelectViewModel(selectModel));
        }

        private async Task<SelectViewModel> GetSelectViewModel(SelectModel selectModel)
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

            var selectViewModel = new SelectViewModel(selectModel);

            if (selectModel.DiagnosticsMode)
            {
                selectViewModel.Diagnostics = RequestHelper.Diagnostics(_context, _configuration, _webHostEnvironment);
            }

            selectModel.SummaryModel = new SummaryModel(selectModel);

            return selectViewModel;
        }
 
        private SelectModel GetSelectModel()
        {
            try
            {
                SelectModel selectModel = JsonConvert.DeserializeObject<SelectModel>(StateHelper.GetSerialisedModel(_context, _configuration)) ?? new SelectModel();
                selectModel.JSON = TextHelper.Decompress(RequestHelper.FormValue("json", string.Empty, _context) ?? String.Empty);
                AssignParentModel(selectModel);
                selectModel.SearchInput = RequestHelper.FormValue("searchInput", string.Empty, _context)?.Trim() ?? String.Empty; 

                if (selectModel.DataSourceType == DataSourceType.JSON)
                {
                    _jsonRepository.UpdateApiRequestParameters(selectModel, _context);
                }
                return selectModel;
            }
            catch(Exception)
            {
                return new SelectModel();
            }
        }
    }
}