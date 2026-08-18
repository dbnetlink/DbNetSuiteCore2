using DbNetSuiteCore.Constants;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Factories.Interfaces;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Services.Interfaces;
using DbNetSuiteCore.ViewModels;
using Newtonsoft.Json;
using System.Data;

namespace DbNetSuiteCore.Services
{
    public class TreeService : ComponentService, IComponentService
    {
        public TreeService(
            IRepositoryFactory repositoryFactory, 
            RazorViewToStringRenderer razorRendererService, 
            IConfiguration configuration, 
            IWebHostEnvironment webHostEnvironment, 
            ILoggerFactory loggerFactory) 
            : base(repositoryFactory, razorRendererService, configuration, webHostEnvironment, loggerFactory)
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

            string viewName = RequestHelper.TriggerName(_context) == TriggerNames.InitialLoad ? "Tree/__Markup" : "Tree/__Content";
            return await View(viewName, await GetTreeViewModel(treeModel));
        }

        private async Task<TreeViewModel> GetTreeViewModel(TreeModel treeModel)
        {
            if (treeModel.DataSourceType == DataSourceType.FileSystem)
            {
                await LoadDirectoryStructure(treeModel);
            }
            else
            {
                if (String.IsNullOrEmpty(treeModel.DataSourcePluginName) == false)
                {
                    try
                    {
                        treeModel.ClearNestedLevels();
                        PluginHelper.InvokeMethod(treeModel.DataSourcePluginName, nameof(IDataSourcePlugin.GetTreeData), treeModel, null, false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error invoking plugin  {nameof(IDataSourcePlugin)} => {nameof(IDataSourcePlugin.GetTreeData)}");
                        throw;
                    }
                }
                else
                {
                    foreach (var level in treeModel.Levels)
                    {
                        await ConfigureColumns(level);
                        await GetRecords(level);
                    }
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
            ISqlRepository repository = _repositoryFactory.GetSqlRepository(treeModel.DataSourceType);

            while (folders.Any())
            {
                var childLevel = treeModel.Levels.Last().DeepCopy();
                treeModel.NestedLevel = childLevel;
                childLevel.Data = GetEmptyDataTable();

                foreach (var folder in folders)
                {
                    childLevel.Url = folder.RowValue(FileSystemColumn.Path).ToString();
                    childLevel.TableName = $"t1_{DateTime.Now.Ticks}";
                    QueryCommandConfig query = childLevel.BuildQuery();
                    var dataTable = await repository.GetDataTable(query, childLevel);

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

        private DataTable GetEmptyDataTable()
        {
            DataTable dataTable = new DataTable();
            dataTable.Clear();
            dataTable.Columns.Add(FileSystemColumn.Icon.ToString(), typeof(bool));
            dataTable.Columns.Add(FileSystemColumn.IsDirectory.ToString(), typeof(bool));
            dataTable.Columns.Add(FileSystemColumn.Name.ToString(), typeof(string));
            dataTable.Columns.Add(FileSystemColumn.Extension.ToString(), typeof(string));
            dataTable.Columns.Add(FileSystemColumn.Length.ToString(), typeof(Int64));
            dataTable.Columns.Add(FileSystemColumn.LastModified.ToString(), typeof(DateTime));
            dataTable.Columns.Add(FileSystemColumn.Folder.ToString(), typeof(string));
            dataTable.Columns.Add(FileSystemColumn.ParentFolder.ToString(), typeof(string));
            dataTable.Columns.Add(FileSystemColumn.Path.ToString(), typeof(string));
            dataTable.Columns.Add(FileSystemColumn.Content.ToString(), typeof(string));
            return dataTable;
        }

        private TreeModel GetTreeModel()
        {
            string json = StateHelper.GetSerialisedModel(_context, _configuration);
            TreeModel treeModel = JsonConvert.DeserializeObject<TreeModel>(json) ?? new TreeModel();
            treeModel.HttpContext = _context;
            UpdateFixedFilterParameters(treeModel);
            UpdateApiRequestParameters(treeModel);
            foreach (var level in treeModel._nestedLevels)
            {
                UpdateFixedFilterParameters(level);
                UpdateApiRequestParameters(level);
            }

            return treeModel;
        }
      
    }
}