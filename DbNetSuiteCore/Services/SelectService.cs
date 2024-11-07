using DbNetSuiteCore.Services.Interfaces;
using DbNetSuiteCore.Repositories;
using System.Text;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Extensions;
using System.Data;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Enums;
using Newtonsoft.Json;
using DbNetSuiteCore.Constants;
using DbNetSuiteCore.ViewModels;
using System.Collections;
using System.Linq;

namespace DbNetSuiteCore.Services
{
    public class SelectService : ComponentService, ISelectService
    {
        public SelectService(IMSSQLRepository msSqlRepository, RazorViewToStringRenderer razorRendererService, ISQLiteRepository sqliteRepository, IJSONRepository jsonRepository, IFileSystemRepository fileSystemRepository, IMySqlRepository mySqlRepository, IPostgreSqlRepository postgreSqlRepository, IExcelRepository excelRepository, IMongoDbRepository mongoDbRepository) : base(msSqlRepository, razorRendererService, sqliteRepository, jsonRepository, fileSystemRepository, mySqlRepository, postgreSqlRepository, excelRepository, mongoDbRepository)
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


            switch (selectModel.TriggerName)
            {
                default:
                    string viewName = selectModel.Uninitialised ? "Select/Markup" : "Select/Options";
                    return await View(viewName, await GetSelectViewModel(selectModel));
            }
        }

        private async Task<Byte[]> View<TModel>(string viewName, TModel model)
        {
            return Encoding.UTF8.GetBytes(await _razorRendererService.RenderViewToStringAsync($"Views/{viewName}.cshtml", model));
        }

        private async Task<SelectViewModel> GetSelectViewModel(SelectModel selectModel)
        {
             if (selectModel.IsStoredProcedure == false && selectModel.Uninitialised)
            {
                await ConfigureSelectColumns(selectModel, selectModel.Columns);
            }
            await GetRecords(selectModel);
            if (selectModel.IsStoredProcedure && selectModel.Uninitialised)
            {
                ConfigureGridColumnsForStoredProcedure(selectModel);
            }

            var gridViewModel = new SelectViewModel(selectModel);

            if (selectModel.DiagnosticsMode)
            {
                gridViewModel.Diagnostics = RequestHelper.Diagnostics(_context);
            }

            return gridViewModel;
        }

        private async Task ConfigureSelectColumns(ComponentModel componentModel, IEnumerable<ColumnModel> columns)
        {
            columns = ColumnsHelper.MoveDataOnlyColumnsToEnd(columns).Cast<SelectColumn>();

            DataTable schema = await GetColumns(componentModel);

            if (columns.Any() == false)
            {
                switch (componentModel.DataSourceType)
                {
                    case DataSourceType.MSSQL:
                        columns = schema.Rows.Cast<DataRow>().Where(r => (bool)r["IsHidden"] == false).Select(r => new SelectColumn(r)).Cast<SelectColumn>().Where(c => c.Valid).ToList();
                        break;
                    default:
                        columns = schema.Columns.Cast<DataColumn>().Select(c => new SelectColumn(c, componentModel.DataSourceType)).Cast<SelectColumn>().ToList();
                        break;
                }

                ColumnsHelper.QualifyColumnExpressions(columns, componentModel.DataSourceType);
            }
            else
            {
                var dataColumns = schema.Columns.Cast<DataColumn>().ToList();

                if (componentModel.DataSourceType == DataSourceType.FileSystem)
                {
                    foreach (SelectColumn selectColumn in columns)
                    {
                        selectColumn.Update(dataColumns.First(dc => dc.ColumnName == selectColumn.Expression), componentModel.DataSourceType);
                    }
                }
                else
                {
                    for (var i = 0; i < dataColumns.Count; i++)
                    {
                        columns.ToList()[i].Update(dataColumns[i], componentModel.DataSourceType);
                    }
                }
            }
            for (var i = 0; i < columns.ToList().Count; i++)
            {
                columns.ToList()[i].Ordinal = i + 1;
            }
        }

        private void ConfigureGridColumnsForStoredProcedure(SelectModel selectModel)
        {
            DataTable schema = selectModel.Data;

            if (selectModel.Columns.Any() == false)
            {
                selectModel.Columns = schema.Columns.Cast<DataColumn>().Select(dc => new SelectColumn(dc, selectModel.DataSourceType)).Cast<SelectColumn>().ToList();
                ColumnsHelper.QualifyColumnExpressions(selectModel.Columns, selectModel.DataSourceType);
            }
            else
            {
                var selectColumns = selectModel.Columns.DeepCopy();
                selectModel.Columns = new List<SelectColumn>();
                var dataColumns = schema.Columns.Cast<DataColumn>().ToList();

                for (var i = 0; i < dataColumns.Count; i++)
                {
                    var dataColumn = dataColumns[i];
                    var selectColumn = selectColumns.FirstOrDefault(c => c.Expression.ToLower() == dataColumn.ColumnName.ToLower());

                    if (selectColumn == null)
                    {
                        selectColumn = new SelectColumn(dataColumn, selectModel.DataSourceType);
                    }
                    else
                    {
                        selectColumn.Update(dataColumn, selectModel.DataSourceType);
                    }
                    selectModel.Columns = selectModel.Columns.Append(selectColumn);
                }
            }
            for (var i = 0; i < selectModel.Columns.ToList().Count; i++)
            {
                selectModel.Columns.ToList()[i].Ordinal = i + 1;
            }
        }

        private async Task GetRecords(SelectModel selectModel)
        {
            switch (selectModel.DataSourceType)
            {
                case DataSourceType.SQLite:
                    await _sqliteRepository.GetRecords(selectModel);
                    break;
                case DataSourceType.MySql:
                    await _mySqlRepository.GetRecords(selectModel);
                    break;
                case DataSourceType.PostgreSql:
                    await _postgreSqlRepository.GetRecords(selectModel);
                    break;
                case DataSourceType.JSON:
                    await _jsonRepository.GetRecords(selectModel, _context);
                    break;
                case DataSourceType.Excel:
                    await _excelRepository.GetRecords(selectModel);
                    break;
                case DataSourceType.FileSystem:
                    await _fileSystemRepository.GetRecords(selectModel, _context);
                    break;
                case DataSourceType.MongoDB:
                    await _mongoDbRepository.GetRecords(selectModel);
                    break;
                default:
                    await _msSqlRepository.GetRecords(selectModel);
                    break;
            }
        }

        private async Task<DataTable> GetColumns(ComponentModel componentModel)
        {
            switch (componentModel.DataSourceType)
            {
                case DataSourceType.SQLite:
                    return await _sqliteRepository.GetColumns(componentModel);
                case DataSourceType.MySql:
                    return await _mySqlRepository.GetColumns(componentModel);
                case DataSourceType.PostgreSql:
                    return await _postgreSqlRepository.GetColumns(componentModel);
                case DataSourceType.JSON:
                    return await _jsonRepository.GetColumns(componentModel, _context);
                case DataSourceType.Excel:
                    return await _excelRepository.GetColumns(componentModel);
                case DataSourceType.FileSystem:
                    return await _fileSystemRepository.GetColumns(componentModel, _context);
                case DataSourceType.MongoDB:
                    return await _mongoDbRepository.GetColumns(componentModel);
                default:
                    return await _msSqlRepository.GetColumns(componentModel);
            }
        }

        private SelectModel GetSelectModel()
        {
            try
            {
                var model = TextHelper.DeobfuscateString(RequestHelper.FormValue("model", string.Empty, _context));
                SelectModel selectModel = JsonConvert.DeserializeObject<SelectModel>(model) ?? new SelectModel();
                selectModel.JSON = TextHelper.Decompress(RequestHelper.FormValue("json", string.Empty, _context)); ;
                selectModel.ParentKey = RequestHelper.FormValue("primaryKey", selectModel.ParentKey, _context);

                return selectModel;
            }
            catch
            {
                return new SelectModel();
            }
        }
    }
}