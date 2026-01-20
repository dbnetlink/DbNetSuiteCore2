using DbNetSuiteCore.Constants;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Services.Interfaces;
using DbNetSuiteCore.ViewModels;
using Newtonsoft.Json;
using System.Data;

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
            if (treeModel.DataSourceType == Enums.DataSourceType.FileSystem)
            {
             //   FileSystemRepository.UpdateUrl(treeModel);
                await LoadDirectoryStructure(treeModel);
            }
            else
            {
                foreach (var level in treeModel.Levels)
                {
                    await ConfigureColumns(level);
                    await GetRecords(level);

                    treeModel.DataTables.Add(level.Data);
                }
            }
            var treeViewModel = new TreeViewModel(treeModel);

            if (treeModel.DiagnosticsMode)
            {
                treeViewModel.Diagnostics = RequestHelper.Diagnostics(_context, _configuration, _webHostEnvironment);
            }

            return treeViewModel;
        }


        private async Task LoadDirectoryStructure(TreeModel treeModel)
        {
            var columns = new List<TreeColumn> {
                    new TreeColumn(FileSystemColumn.Path.ToString()) { },
                    new TreeColumn(FileSystemColumn.ParentFolder.ToString()) {  ForeignKey = true },
                    new TreeColumn(FileSystemColumn.IsDirectory.ToString())
                };

            foreach (var column in columns)
            {
                treeModel.Columns = treeModel.Columns.ToList().Append(column);
            }
            await ConfigureColumns(treeModel.Levels.Last());
            await GetRecords(treeModel.Levels.Last());

            var folders = treeModel.Levels.Last().Data.Rows.Cast<DataRow>().Where(r => Convert.ToBoolean(r.RowValue(FileSystemColumn.IsDirectory))).ToList();

            while (folders.Any())
            {
                var childLevel = treeModel.Levels.Last().DeepCopy();
                treeModel.NestedLevel = childLevel;
                childLevel.Data = _fileSystemRepository.GetEmptyDataTable();

                foreach (var folder in folders)
                {
                    var dataTable = _fileSystemRepository.GetFolderContents(folder.RowValue(FileSystemColumn.Path).ToString(), childLevel);
                    foreach (DataRow row in dataTable.Rows)
                    {
                        DataRow newRow = childLevel.Data.NewRow();
                        newRow.ItemArray = row.ItemArray;
                        childLevel.Data.Rows.Add(newRow);
                    }
                }   
              
                folders = childLevel.Data.Rows.Cast<DataRow>().Where(r => Convert.ToBoolean(r.RowValue(FileSystemColumn.IsDirectory))).ToList();
            }
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