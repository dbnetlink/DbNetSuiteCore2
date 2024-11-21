using DbNetSuiteCore.Services.Interfaces;
using DbNetSuiteCore.Repositories;
using System.Text;
using DbNetSuiteCore.Models;
using System.Data;
using DbNetSuiteCore.Helpers;
using Newtonsoft.Json;
using DbNetSuiteCore.ViewModels;
using DbNetSuiteCore.Constants;

namespace DbNetSuiteCore.Services
{
    public class SelectService : ComponentService, IComponentService
    {
        public SelectService(IMSSQLRepository msSqlRepository, RazorViewToStringRenderer razorRendererService, ISQLiteRepository sqliteRepository, IJSONRepository jsonRepository, IFileSystemRepository fileSystemRepository, IMySqlRepository mySqlRepository, IPostgreSqlRepository postgreSqlRepository, IExcelRepository excelRepository, IMongoDbRepository mongoDbRepository, IConfiguration configuration) : base(msSqlRepository, razorRendererService, sqliteRepository, jsonRepository, fileSystemRepository, mySqlRepository, postgreSqlRepository, excelRepository, mongoDbRepository, configuration)
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
                return await View("Error", ex);
            }
        }

        private async Task<Byte[]> SelectView()
        {
            SelectModel selectModel = GetSelectModel() ?? new SelectModel();
            selectModel.TriggerName = RequestHelper.TriggerName(_context);

            ValidateModel(selectModel);

            switch (selectModel.TriggerName)
            {
                default:
                    string viewName = selectModel.Uninitialised ? "Select/Markup" : "Select/Options";
                    return await View(viewName, await GetSelectViewModel(selectModel));
            }
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

            var gridViewModel = new SelectViewModel(selectModel);

            if (selectModel.DiagnosticsMode)
            {
                gridViewModel.Diagnostics = RequestHelper.Diagnostics(_context);
            }

            return gridViewModel;
        }
 
        private SelectModel GetSelectModel()
        {
            try
            {
                var model = TextHelper.DeobfuscateString(RequestHelper.FormValue("model", string.Empty, _context),_configuration);
                SelectModel selectModel = JsonConvert.DeserializeObject<SelectModel>(model) ?? new SelectModel();
                selectModel.JSON = TextHelper.Decompress(RequestHelper.FormValue("json", string.Empty, _context));
                AssignParentKey(selectModel);
                selectModel.SearchInput = RequestHelper.FormValue("searchInput", string.Empty, _context).Trim();
                return selectModel;
            }
            catch
            {
                return new SelectModel();
            }
        }
    }
}