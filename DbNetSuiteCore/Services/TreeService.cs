using DbNetSuiteCore.Constants;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Services.Interfaces;
using DbNetSuiteCore.ViewModels;
using Newtonsoft.Json;
using System.Web;

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
                    case "treecontrol":
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

            string viewName = treeModel.Uninitialised ? "Tree/__Markup" : "Tree/__Content";
            return await View(viewName, await GetTreeViewModel(treeModel));
        }

        private async Task<TreeViewModel> GetTreeViewModel(TreeModel treeModel)
        {
            foreach (var level in treeModel.Levels)
            {
                await ConfigureColumns(level);
                await GetRecords(level);

                treeModel.DataTables.Add(level.Data);
            }

            var treeViewModel = new TreeViewModel(treeModel);

            if (treeModel.DiagnosticsMode)
            {
                treeViewModel.Diagnostics = RequestHelper.Diagnostics(_context, _configuration, _webHostEnvironment);
            }

            return treeViewModel;
        }


        private TreeModel GetTreeModel()
        {
            string json = StateHelper.GetSerialisedModel(_context, _configuration);
            TreeModel treeModel = JsonConvert.DeserializeObject<TreeModel>(json) ?? new TreeModel();
            treeModel.HttpContext = _context;
            UpdateFixedFilterParameters(treeModel);
            foreach (var level in treeModel._nestedLevels)
            {
                UpdateFixedFilterParameters(level);
            }
            return treeModel;
        }

        public void UpdateFixedFilterParameters(TreeModel treeModel)
        {
            Dictionary<string, object> fixedFilterParemeters = JsonConvert.DeserializeObject<Dictionary<string, object>>(RequestHelper.FormValue(TriggerNames.FixedFilterParameters, string.Empty, _context)) ?? new Dictionary<string, object>();
            fixedFilterParemeters = new Dictionary<string, object>(fixedFilterParemeters, StringComparer.OrdinalIgnoreCase);
            foreach (DbParameter dbParameter in treeModel.FixedFilterParameters)
            {
                if (fixedFilterParemeters.ContainsKey(dbParameter.Name))
                {
                    dbParameter.Value = fixedFilterParemeters[dbParameter.Name];
                }
            }
        }
    }
}