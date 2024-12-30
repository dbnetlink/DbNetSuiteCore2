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
    public class GridService : ComponentService, IComponentService
    {
        public GridService(IMSSQLRepository msSqlRepository, RazorViewToStringRenderer razorRendererService, ISQLiteRepository sqliteRepository, IJSONRepository jsonRepository, IFileSystemRepository fileSystemRepository, IMySqlRepository mySqlRepository, IPostgreSqlRepository postgreSqlRepository, IExcelRepository excelRepository, IMongoDbRepository mongoDbRepository, IConfiguration configuration) : base(msSqlRepository, razorRendererService, sqliteRepository, jsonRepository, fileSystemRepository, mySqlRepository, postgreSqlRepository, excelRepository, mongoDbRepository, configuration)
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
                return await View("__Error", ex);
            }
        }

        private async Task<Byte[]> GridView()
        {
            GridModel gridModel = GetGridModel() ?? new GridModel();
            gridModel.TriggerName = RequestHelper.TriggerName(_context);

            switch (gridModel.TriggerName)
            {
                case TriggerNames.Download:
                    return await ExportRecords(gridModel);
                case TriggerNames.NestedGrid:
                    return await View("Grid/__Nested", ConfigureNestedGrid(gridModel));
                case TriggerNames.ViewDialogContent:
                    return await View("Grid/__ViewDialogContent", await ViewDialogContent(gridModel));
                default:
                    string viewName = gridModel.Uninitialised ? "Grid/__Markup" : "Grid/__Rows";
                    return await View(viewName, await GetGridViewModel(gridModel));
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
                await ConfigureColumns(gridModel);
            }
            await GetGridRecords(gridModel);
            if (gridModel.IsStoredProcedure && gridModel.Uninitialised)
            {
                ConfigureColumnsForStoredProcedure(gridModel);
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
                nestedGrid.Caption = string.Empty;
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

        private async Task GetGridRecords(GridModel gridModel)
        {
            gridModel.ConfigureSort(RequestHelper.FormValue("sortKey", string.Empty, _context));

            switch (gridModel.TriggerName)
            {
                case TriggerNames.ParentKey:
                case TriggerNames.Refresh:
                    gridModel.ColumnFilter = gridModel.ColumnFilter.Select(s => s = string.Empty).ToList();
                    gridModel.Columns.ToList().ForEach(c => c.DbLookupOptions = null);
                    break;
            }

            await GetRecords(gridModel);
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
            return await View("Grid/__Export", gridViewModel);
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

                        string value = row[dataColumn]?.ToString() ?? string.Empty;
                        var cell = worksheet.Cell(rowIdx, colIdx);
                        cell.Value = column.FormatValue(value)?.ToString() ?? string.Empty;

                        if (columnWidths.ContainsKey(column.ColumnName))
                        {
                            if (value.Length > columnWidths[column.ColumnName])
                            {
                                columnWidths[column.ColumnName] = value.Length;
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
                var model = TextHelper.DeobfuscateString(RequestHelper.FormValue("model", string.Empty, _context), _configuration);
                GridModel gridModel = JsonConvert.DeserializeObject<GridModel>(model) ?? new GridModel();
                gridModel.JSON = TextHelper.Decompress(RequestHelper.FormValue("json", string.Empty, _context)); ;
                gridModel.CurrentPage = gridModel.ToolbarPosition == ToolbarPosition.Hidden ? 1 : GetPageNumber(gridModel);
                gridModel.SearchInput = RequestHelper.FormValue("searchInput", string.Empty, _context).Trim();
                gridModel.SortKey = RequestHelper.FormValue("sortKey", gridModel.SortKey, _context);
                gridModel.ExportFormat = RequestHelper.FormValue("exportformat", string.Empty, _context);
                gridModel.ColumnFilter = RequestHelper.FormValueList("columnFilter", _context).Select(f => f.Trim()).ToList();
                AssignParentKey(gridModel);

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
                case TriggerNames.Refresh:
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