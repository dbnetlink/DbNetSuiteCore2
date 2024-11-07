using DbNetSuiteCore.Services.Interfaces;
using DbNetSuiteCore.Repositories;
using System.Text;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Extensions;
using System.Data;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.StaticFiles;
using ClosedXML.Excel;
using Newtonsoft.Json;
using DbNetSuiteCore.Constants;
using DbNetSuiteCore.ViewModels;

namespace DbNetSuiteCore.Services
{
    public class GridService : ComponentService, IGridService
    {
        public GridService(IMSSQLRepository msSqlRepository, RazorViewToStringRenderer razorRendererService, ISQLiteRepository sqliteRepository, IJSONRepository jsonRepository, IFileSystemRepository fileSystemRepository, IMySqlRepository mySqlRepository, IPostgreSqlRepository postgreSqlRepository, IExcelRepository excelRepository, IMongoDbRepository mongoDbRepository) : base(msSqlRepository, razorRendererService, sqliteRepository, jsonRepository, fileSystemRepository, mySqlRepository, postgreSqlRepository, excelRepository, mongoDbRepository)
        {
        }

        public async Task<Byte[]> Process(HttpContext context, string page)
        {
            try
            {
                _context = context;
                switch (page.ToLower())
                {
                    case "gridcontrol":
                        return await GridView();
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

        private async Task<Byte[]> GridView()
        {
            GridModel gridModel = GetGridModel() ?? new GridModel();

            gridModel.TriggerName = RequestHelper.TriggerName(_context);

            if (gridModel.TriggerName == TriggerNames.InitialLoad)
            {
                ValidateGridModel(gridModel);
            }

            switch (gridModel.TriggerName)
            {
                case TriggerNames.Download:
                    return await ExportRecords(gridModel);
                case TriggerNames.NestedGrid:
                    return await View("Grid/Nested", ConfigureNestedGrid(gridModel));
                case TriggerNames.ViewDialogContent:
                    return await View("Grid/ViewDialogContent", await ViewDialogContent(gridModel));
                default:
                    string viewName = gridModel.Uninitialised ? "Grid/Markup" : "Grid/Rows";
                    return await View(viewName, await GetGridViewModel(gridModel));
            }
        }

        private void ValidateGridModel(GridModel gridModel)
        {
            var primaryKeyAssigned = gridModel.Columns.Any(x => x.PrimaryKey);
            if (gridModel.ViewDialog != null && primaryKeyAssigned == false)
            {
                throw new Exception("A column designated as a primary key is required for the view dialog");
            }
            if (gridModel.DataSourceType == DataSourceType.MongoDB && string.IsNullOrEmpty(gridModel.DatabaseName))
            {
                throw new Exception("The DatabaseName property must also be supplied for MongoDB connections");
            }
        }

        private async Task<GridViewDialogViewModel> ViewDialogContent(GridModel gridModel)
        {
            gridModel.ParentKey = RequestHelper.FormValue(TriggerNames.ViewDialogContent, "", _context);
            await GetRecord(gridModel);
            return new GridViewDialogViewModel(gridModel);
        }

        private async Task<GridViewModel> GetGridViewModel(GridModel gridModel)
        {
            if (gridModel.DataSourceType == DataSourceType.FileSystem && string.IsNullOrEmpty(gridModel.ParentKey) == false)
            {
                FileSystemRepository.UpdateUrl(gridModel);
            }

            if (gridModel.IsStoredProcedure == false && gridModel.Uninitialised)
            {
                await ConfigureGridColumns(gridModel);
            }
            await GetRecords(gridModel);
            if (gridModel.IsStoredProcedure && gridModel.Uninitialised)
            {
                ConfigureGridColumnsForStoredProcedure(gridModel);
            }

            gridModel.CurrentSortKey = RequestHelper.FormValue("sortKey", gridModel.CurrentSortKey, _context);

            var gridViewModel = new GridViewModel(gridModel);

            if (gridModel.DiagnosticsMode)
            {
                gridViewModel.Diagnostics = RequestHelper.Diagnostics(_context);
            }

            return gridViewModel;
        }

        private List<GridModel> ConfigureNestedGrid(GridModel gridModel)
        {
            foreach (var nestedGrid in gridModel._NestedGrids)
            {
                nestedGrid.IsNested = true;
                nestedGrid.ParentKey = RequestHelper.FormValue("primaryKey", "", _context);
                nestedGrid.SetId();

                if (gridModel.DataSourceType == DataSourceType.FileSystem)
                {
                    var nestedChildGrid = gridModel._NestedGrids.First().DeepCopy();
                    nestedChildGrid.Url = $"{nestedChildGrid.Url}/{nestedGrid.ParentKey}";
                    nestedGrid._NestedGrids.Add(nestedChildGrid);
                }
                else if (string.IsNullOrEmpty(gridModel.ConnectionAlias) == false)
                {
                    nestedGrid.ConnectionAlias = gridModel.ConnectionAlias;
                    nestedGrid.DataSourceType = gridModel.DataSourceType;
                }
            }

            return gridModel._NestedGrids;
        }

        private async Task ConfigureGridColumns(GridModel gridModel)
        {
            gridModel.Columns = ColumnsHelper.MoveDataOnlyColumnsToEnd(gridModel.Columns.Cast<ColumnModel>()).Cast<GridColumn>().ToList();

            DataTable schema = await GetColumns(gridModel);

            if (gridModel.Columns.Any() == false)
            {
                switch (gridModel.DataSourceType)
                {
                    case DataSourceType.MSSQL:
                        gridModel.Columns = schema.Rows.Cast<DataRow>().Where(r => (bool)r["IsHidden"] == false).Select(r => new GridColumn(r)).Cast<GridColumn>().Where(c => c.Valid).ToList();
                        break;
                    default:
                        gridModel.Columns = schema.Columns.Cast<DataColumn>().Select(c => new GridColumn(c, gridModel.DataSourceType)).Cast<GridColumn>().ToList();
                        break;
                }

                ColumnsHelper.QualifyColumnExpressions(gridModel.Columns, gridModel.DataSourceType);
            }
            else
            {
                var dataColumns = schema.Columns.Cast<DataColumn>().ToList();

                if (gridModel.DataSourceType == DataSourceType.FileSystem)
                {
                    foreach (GridColumn gridColumn in gridModel.Columns)
                    {
                        gridColumn.Update(dataColumns.First(dc => dc.ColumnName == gridColumn.Expression), gridModel.DataSourceType);
                    }
                }
                else
                {
                    for (var i = 0; i < dataColumns.Count; i++)
                    {
                        gridModel.Columns.ToList()[i].Update(dataColumns[i], gridModel.DataSourceType);
                    }
                }
            }
            for (var i = 0; i < gridModel.Columns.ToList().Count; i++)
            {
                gridModel.Columns.ToList()[i].Ordinal = i + 1;
            }
        }

        private void ConfigureGridColumnsForStoredProcedure(GridModel gridModel)
        {
            DataTable schema = gridModel.Data;

            if (gridModel.Columns.Any() == false)
            {
                gridModel.Columns = schema.Columns.Cast<DataColumn>().Select(dc => new GridColumn(dc, gridModel.DataSourceType)).Cast<GridColumn>().ToList();
                ColumnsHelper.QualifyColumnExpressions(gridModel.Columns, gridModel.DataSourceType);
            }
            else
            {
                var gridColumns = gridModel.Columns.DeepCopy();
                gridModel.Columns = new List<GridColumn>();
                var dataColumns = schema.Columns.Cast<DataColumn>().ToList();

                for (var i = 0; i < dataColumns.Count; i++)
                {
                    var dataColumn = dataColumns[i];
                    var gridColumn = gridColumns.FirstOrDefault(c => c.Expression.ToLower() == dataColumn.ColumnName.ToLower());

                    if (gridColumn == null)
                    {
                        gridColumn = new GridColumn(dataColumn, gridModel.DataSourceType);
                    }
                    else
                    {
                        gridColumn.Update(dataColumn, gridModel.DataSourceType);
                    }
                    gridModel.Columns = gridModel.Columns.Append(gridColumn);
                }
            }
            for (var i = 0; i < gridModel.Columns.ToList().Count; i++)
            {
                gridModel.Columns.ToList()[i].Ordinal = i + 1;
            }
        }

        private async Task GetRecord(GridModel gridModel)
        {
            /*
            gridModel.ConfigureSort(RequestHelper.FormValue("sortKey", string.Empty, _context));

            if (gridModel.TriggerName == TriggerNames.LinkedGrid)
            {
                gridModel.ColumnFilter = gridModel.ColumnFilter.Select(s => s = string.Empty).ToList();
            }
            */
            switch (gridModel.DataSourceType)
            {
                case DataSourceType.SQLite:
                    await _sqliteRepository.GetRecord(gridModel);
                    break;
                case DataSourceType.MySql:
                    await _mySqlRepository.GetRecord(gridModel);
                    break;
                case DataSourceType.PostgreSql:
                    await _postgreSqlRepository.GetRecord(gridModel);
                    break;
                case DataSourceType.JSON:
                    await _jsonRepository.GetRecord(gridModel, _context);
                    break;
                case DataSourceType.Excel:
                    await _excelRepository.GetRecord(gridModel);
                    break;
                case DataSourceType.MongoDB:
                    await _mongoDbRepository.GetRecord(gridModel);
                    break;
                default:
                    await _msSqlRepository.GetRecord(gridModel);
                    break;
            }
        }

        private async Task GetRecords(GridModel gridModel)
        {
            gridModel.ConfigureSort(RequestHelper.FormValue("sortKey", string.Empty, _context));

            if (gridModel.TriggerName == TriggerNames.LinkedGrid)
            {
                gridModel.ColumnFilter = gridModel.ColumnFilter.Select(s => s = string.Empty).ToList();
            }

            switch (gridModel.DataSourceType)
            {
                case DataSourceType.SQLite:
                    await _sqliteRepository.GetRecords(gridModel);
                    break;
                case DataSourceType.MySql:
                    await _mySqlRepository.GetRecords(gridModel);
                    break;
                case DataSourceType.PostgreSql:
                    await _postgreSqlRepository.GetRecords(gridModel);
                    break;
                case DataSourceType.JSON:
                    await _jsonRepository.GetRecords(gridModel, _context);
                    break;
                case DataSourceType.Excel:
                    await _excelRepository.GetRecords(gridModel);
                    break;
                case DataSourceType.FileSystem:
                    await _fileSystemRepository.GetRecords(gridModel, _context);
                    break;
                case DataSourceType.MongoDB:
                    await _mongoDbRepository.GetRecords(gridModel);
                    break;
                default:
                    await _msSqlRepository.GetRecords(gridModel);
                    break;
            }
        }

        private async Task<DataTable> GetColumns(GridModel gridModel)
        {
            switch (gridModel.DataSourceType)
            {
                case DataSourceType.SQLite:
                    return await _sqliteRepository.GetColumns(gridModel);
                case DataSourceType.MySql:
                    return await _mySqlRepository.GetColumns(gridModel);
                case DataSourceType.PostgreSql:
                    return await _postgreSqlRepository.GetColumns(gridModel);
                case DataSourceType.JSON:
                    return await _jsonRepository.GetColumns(gridModel, _context);
                case DataSourceType.Excel:
                    return await _excelRepository.GetColumns(gridModel);
                case DataSourceType.FileSystem:
                    return await _fileSystemRepository.GetColumns(gridModel, _context);
                case DataSourceType.MongoDB:
                    return await _mongoDbRepository.GetColumns(gridModel);
                default:
                    return await _msSqlRepository.GetColumns(gridModel);
            }
        }

        private async Task<Byte[]> ExportRecords(GridModel gridModel)
        {
            await GetRecords(gridModel);
            switch (gridModel.ExportFormat)
            {
                case "csv":
                    return ConvertDataTableToCSV(gridModel);
                case "excel":
                    return ConvertDataTableToSpreadsheet(gridModel);
                case "json":
                    return ConvertDataTableToJSON(gridModel.Data);
                default:
                    return await ConvertDataTableToHTML(gridModel);
            }
        }

        private DataTable TransformData(GridModel gridModel)
        {
            DataTable dataTable = gridModel.Data.Clone();
            foreach (DataColumn dataColumn in dataTable.Columns)
            {
                dataColumn.DataType = typeof(String);
            }

            foreach (DataRow row in gridModel.Data.Rows)
            {
                DataRow dataRow = dataTable.NewRow();

                foreach (GridColumn columnModel in gridModel.Columns)
                {
                    DataColumn? dataColumn = gridModel.GetDataColumn(columnModel);

                    if (dataColumn != null)
                    {
                        dataRow[dataColumn] = columnModel.FormatValue(row[dataColumn]);
                    }
                }
                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }
        private Byte[] ConvertDataTableToCSV(GridModel gridModel)
        {
            StringBuilder sb = new StringBuilder();

            IEnumerable<string> columnNames = gridModel.Data.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in gridModel.Data.Rows)
            {
                IEnumerable<string> fields = new List<string>();
                foreach (GridColumn columnModel in gridModel.Columns)
                {
                    DataColumn? dataColumn = gridModel.GetDataColumn(columnModel);

                    if (dataColumn != null)
                    {
                        string value = string.Concat("\"", columnModel.FormatValue(row[dataColumn])?.ToString()?.Replace("\"", "\"\""), "\"") ?? string.Empty;
                        fields = fields.Append(value);
                    }
                }
                sb.AppendLine(string.Join(",", fields));
            }
            _context.Response.ContentType = GetMimeTypeForFileExtension(".csv");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private async Task<Byte[]> ConvertDataTableToHTML(GridModel gridModel)
        {
            gridModel.PageSize = gridModel.Data.Rows.Count;
            _context.Response.ContentType = GetMimeTypeForFileExtension(".html");
            var gridViewModel = await GetGridViewModel(gridModel);
            gridViewModel.RenderMode = RenderMode.Export;
            return await View("Grid/Export", gridViewModel);
        }

        private byte[] ConvertDataTableToJSON(DataTable dataTable)
        {
            var json = JsonConvert.SerializeObject(dataTable, Newtonsoft.Json.Formatting.Indented);
            _context.Response.ContentType = GetMimeTypeForFileExtension(".json");
            return Encoding.UTF8.GetBytes(json);
        }

        private byte[] ConvertDataTableToSpreadsheet(GridModel gridModel)
        {
            using (XLWorkbook workbook = new XLWorkbook())
            {
                Dictionary<string, int> columnWidths = new Dictionary<string, int>();

                var worksheet = workbook.Worksheets.Add(gridModel.Id);

                var rowIdx = 1;
                var colIdx = 1;
                foreach (var column in gridModel.Columns)
                {
                    var cell = worksheet.Cell(rowIdx, colIdx);
                    cell.Value = column.Label;
                    cell.Style.Font.Bold = true;
                    worksheet.Column(colIdx).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

                    if (column.IsNumeric)
                    {
                        worksheet.Column(colIdx).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                    }
                    else
                    {
                        switch (column.DataTypeName)
                        {
                            case nameof(DateTime):
                            case nameof(TimeSpan):
                                worksheet.Column(colIdx).Width = 10;
                                break;
                            case nameof(System.Boolean):
                                break;
                            default:
                                columnWidths[column.ColumnName] = 0;
                                break;
                        }
                    }
                    colIdx++;
                }

                foreach (DataRow row in gridModel.Data.Rows)
                {
                    rowIdx++;
                    colIdx = 1;

                    foreach (var column in gridModel.Columns)
                    {
                        DataColumn? dataColumn = gridModel.GetDataColumn(column);

                        if (dataColumn == null)
                        {
                            continue;
                        }

                        object value = row[dataColumn];
                        var cell = worksheet.Cell(rowIdx, colIdx);
                        cell.Value = column.FormatValue(value)?.ToString() ?? string.Empty;

                        if (columnWidths.ContainsKey(column.ColumnName))
                        {
                            if (value.ToString().Length > columnWidths[column.ColumnName])
                            {
                                columnWidths[column.ColumnName] = value.ToString().Length;
                            }
                        }

                        colIdx++;
                    }
                }

                colIdx = 0;
                foreach (var column in gridModel.Columns)
                {
                    colIdx++;

                    if (columnWidths.ContainsKey(column.ColumnName))
                    {
                        var width = columnWidths[column.ColumnName];

                        if (width < 10)
                        {
                            continue;
                        }

                        if (width > 50)
                        {
                            width = 50;
                        }
                        worksheet.Column(colIdx).Width = width * 0.8;
                    }
                }

                _context.Response.ContentType = GetMimeTypeForFileExtension(".xlsx");
                using (var memoryStream = new MemoryStream())
                {
                    workbook.SaveAs(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    return memoryStream.ToArray();
                }
            }
        }

        private GridModel GetGridModel()
        {
            try
            {
                var model = TextHelper.DeobfuscateString(RequestHelper.FormValue("model", string.Empty, _context));
                GridModel gridModel = JsonConvert.DeserializeObject<GridModel>(model) ?? new GridModel();
                gridModel.JSON = TextHelper.Decompress(RequestHelper.FormValue("json", string.Empty, _context)); ;
                gridModel.CurrentPage = gridModel.ToolbarPosition == ToolbarPosition.Hidden ? 1 : GetPageNumber(gridModel);
                gridModel.SearchInput = RequestHelper.FormValue("searchInput", string.Empty, _context).Trim();
                gridModel.SortKey = RequestHelper.FormValue("sortKey", gridModel.SortKey, _context);
                gridModel.ExportFormat = RequestHelper.FormValue("exportformat", string.Empty, _context);
                gridModel.ColumnFilter = RequestHelper.FormValueList("columnFilter", _context).Select(f => f.Trim()).ToList();
                gridModel.ParentKey = RequestHelper.FormValue("primaryKey", gridModel.ParentKey, _context);

                gridModel.Columns.ToList().ForEach(column => column.FilterError = string.Empty);

                return gridModel;
            }
            catch
            {
                return new GridModel();
            }
        }
        private int GetPageNumber(GridModel gridModel)
        {
            switch (RequestHelper.TriggerName(_context))
            {
                case TriggerNames.Page:
                    return Convert.ToInt32(RequestHelper.FormValue("page", "1", _context));
                case TriggerNames.Search:
                case TriggerNames.First:
                case TriggerNames.ColumnFilter:
                    return 1;
                case TriggerNames.Next:
                    return gridModel.CurrentPage + 1;
                case TriggerNames.Previous:
                    return gridModel.CurrentPage - 1;
                case TriggerNames.Last:
                    return Int32.MaxValue;
            }

            return 1;
        }


        private string GetMimeTypeForFileExtension(string extension)
        {
            const string defaultContentType = "application/octet-stream";

            var provider = new FileExtensionContentTypeProvider();

            if (!provider.TryGetContentType(extension, out string contentType))
            {
                contentType = defaultContentType;
            }

            return contentType;
        }
    }
}