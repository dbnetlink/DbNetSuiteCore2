using DbNetSuiteCore.Services.Interfaces;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Helpers;
using Newtonsoft.Json;
using DbNetSuiteCore.ViewModels;
using DbNetSuiteCore.Enums;

namespace DbNetSuiteCore.Services
{
    public class TreeService : ComponentService, IComponentService
    {
        public TreeService(IMSSQLRepository msSqlRepository, RazorViewToStringRenderer razorRendererService, ISQLiteRepository sqliteRepository, IJSONRepository jsonRepository, IFileSystemRepository fileSystemRepository, IMySqlRepository mySqlRepository, IPostgreSqlRepository postgreSqlRepository, IExcelRepository excelRepository, IMongoDbRepository mongoDbRepository, IOracleRepository oracleRepository, IConfiguration configuration, IWebHostEnvironment webHostEnvironment, ILoggerFactory loggerFactory) : base(msSqlRepository, razorRendererService, sqliteRepository, jsonRepository, fileSystemRepository, mySqlRepository, postgreSqlRepository, excelRepository, mongoDbRepository, oracleRepository, configuration, webHostEnvironment, loggerFactory)
        {
        }

    public async Task<Byte[]> Process(HttpContext context, string page)
    {
            try
            {
                _context = context;
                switch (page.ToLower())
                {
                    case "Treecontrol":
                        return await TreeView();
                    default:
                        return new byte[0];
                }
            }
            catch (Exception ex)
            {   
                return await HandleError(ex, context);
            }
        }

        private async Task<Byte[]> TreeView()
        {
            TreeModel treeModel = GetTreeModel() ?? new TreeModel();
            treeModel.TriggerName = _context == null ? string.Empty : RequestHelper.TriggerName(_context);

            string viewName = treeModel.Uninitialised ? "Tree/__Markup" : "Tree/__Options";
            return await View(viewName, await GetTreeViewModel(treeModel));
        }

        private async Task<TreeViewModel> GetTreeViewModel(TreeModel treeModel)
        {
             if (treeModel.Uninitialised)
            {
                await ConfigureColumns(treeModel);
            }
            await GetRecords(treeModel);

            var treeViewModel = new TreeViewModel(treeModel);

            if (treeModel.DiagnosticsMode)
            {
                treeViewModel.Diagnostics = RequestHelper.Diagnostics(_context, _configuration, _webHostEnvironment);
            }

            return treeViewModel;
        }
 
        private TreeModel GetTreeModel()
        {
            try
            {
                TreeModel TreeModel = JsonConvert.DeserializeObject<TreeModel>(StateHelper.GetSerialisedModel(_context, _configuration)) ?? new TreeModel();
                TreeModel.JSON = TextHelper.Decompress(RequestHelper.FormValue("json", string.Empty, _context));
                AssignParentModel(TreeModel);
                TreeModel.SearchInput = RequestHelper.FormValue("searchInput", string.Empty, _context).Trim(); 
  
                if (TreeModel.DataSourceType == DataSourceType.JSON)
                {
                    _jsonRepository.UpdateApiRequestParameters(TreeModel, _context);
                }
                return TreeModel;
            }
            catch(Exception)
            {
                return new TreeModel();
            }
        }
    }
}