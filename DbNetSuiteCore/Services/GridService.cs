using DbNetSuiteCore.Services.Interfaces;
using DbNetSuiteCore.Repositories;
using System.Reflection;
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
using System.Linq;

namespace DbNetSuiteCore.Services
{
    public class GridService : IGridService
    {
        private readonly IMSSQLRepository _msSqlRepository;
        private readonly ISQLiteRepository _sqliteRepository;
        private readonly RazorViewToStringRenderer _razorRendererService;
        private readonly IJSONRepository _jsonRepository;
        private readonly IFileSystemRepository _fileSystemRepository;
        private readonly IMySqlRepository _mySqlRepository;
        private readonly IPostgreSqlRepository _postgreSqlRepository;
        private readonly IExcelRepository _excelRepository;
        private HttpContext? _context = null;
        private string triggerName => _context.Request.Headers.Keys.Contains(HeaderNames.HxTriggerName) ? _context.Request.Headers[HeaderNames.HxTriggerName] : string.Empty;

        public GridService(IMSSQLRepository msSqlRepository, RazorViewToStringRenderer razorRendererService, ISQLiteRepository sqliteRepository, IJSONRepository jsonRepository, IFileSystemRepository fileSystemRepository, IMySqlRepository mySqlRepository, IPostgreSqlRepository postgreSqlRepository, IExcelRepository excelRepository)
        {
            _msSqlRepository = msSqlRepository;
            _razorRendererService = razorRendererService;
            _sqliteRepository = sqliteRepository;
            _jsonRepository = jsonRepository;
            _fileSystemRepository = fileSystemRepository;
            _mySqlRepository = mySqlRepository;
            _postgreSqlRepository = postgreSqlRepository;
            _excelRepository = excelRepository;
        }

        public async Task<Byte[]> Process(HttpContext context, string page)
        {
            try
            {
                _context = context;
                switch (page.ToLower())
                {
                    case "css":
                    case "js":
                        return GetResources(page);
                    case "gridcontrol":
                        return await GridView();
                    default:
                        return GetResource(page.Split(".").Last(), page.Split(".").First());
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

            gridModel.TriggerName = triggerName;

            if (triggerName == TriggerNames.InitialLoad)
            {
                ValidateGridModel(gridModel);
            }

            switch (triggerName)
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
            if (gridModel.ViewDialog && primaryKeyAssigned == false)
            {
                throw new Exception("A column designated as a primary key is required for the view dialog");
            }
        }
        private async Task<Byte[]> View<TModel>(string viewName, TModel model)
        {
            return Encoding.UTF8.GetBytes(await _razorRendererService.RenderViewToStringAsync($"Views/{viewName}.cshtml", model));
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

            return new GridViewModel(gridModel);
        }

        private GridModel ConfigureNestedGrid(GridModel gridModel)
        {
            gridModel.NestedGrid!.IsNested = true;
            gridModel.NestedGrid!.ParentKey = RequestHelper.FormValue("primaryKey", "", _context);
            gridModel.NestedGrid.SetId();

            if (gridModel.DataSourceType == DataSourceType.FileSystem)
            {
                gridModel.NestedGrid.NestedGrid = gridModel.NestedGrid.DeepCopy();
            }
            else if (string.IsNullOrEmpty(gridModel.ConnectionAlias) == false)
            {
                gridModel.NestedGrid.ConnectionAlias = gridModel.ConnectionAlias;
                gridModel.NestedGrid.DataSourceType = gridModel.DataSourceType;
            }

            return gridModel.NestedGrid;
        }

        private async Task ConfigureGridColumns(GridModel gridModel)
        {
            DataTable schema = await GetColumns(gridModel);

            if (gridModel.Columns.Any() == false)
            {
                switch (gridModel.DataSourceType)
                {
                    case DataSourceType.MSSQL:
                        gridModel.Columns = schema.Rows.Cast<DataRow>().Select(r => new GridColumn(r)).Cast<GridColumn>().Where(c => c.Valid).ToList();
                        break;
                    default:
                        gridModel.Columns = schema.Columns.Cast<DataColumn>().Select(c => new GridColumn(c, gridModel.DataSourceType)).Cast<GridColumn>().ToList();
                        break;
                }

                gridModel.QualifyColumnExpressions();
            }
            else
            {
                switch (gridModel.DataSourceType)
                {
                    case DataSourceType.MSSQL:
                        var dataRows = schema.Rows.Cast<DataRow>().ToList();
                        for (var i = 0; i < dataRows.Count; i++)
                        {
                            gridModel.Columns.ToList()[i].Update(dataRows[i]);
                        }
                        break;
                    default:
                        var dataColumns = schema.Columns.Cast<DataColumn>().ToList();

                        if (gridModel.DataSourceType == DataSourceType.FileSystem)
                        {
                            foreach (GridColumn gridColumn in gridModel.Columns)
                            {
                                gridColumn.Update(dataColumns.First(dc => dc.ColumnName == gridColumn.Expression));
                            }
                        }
                        else
                        {
                            for (var i = 0; i < dataColumns.Count; i++)
                            {
                                gridModel.Columns.ToList()[i].Update(dataColumns[i]);
                            }
                        }
                        break;
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
                gridModel.QualifyColumnExpressions();
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
                        gridColumn.Update(dataColumn);
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
            gridModel.ConfigureSort(RequestHelper.FormValue("sortKey", string.Empty, _context));

            if (gridModel.TriggerName == TriggerNames.LinkedGrid)
            {
                gridModel.ColumnFilter = gridModel.ColumnFilter.Select(s => s = string.Empty).ToList();
            }

            switch (gridModel.DataSourceType)
            {
                case DataSourceType.SQlite:
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
                case DataSourceType.SQlite:
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
                default:
                    await _msSqlRepository.GetRecords(gridModel);
                    break;
            }
        }

        private async Task<DataTable> GetColumns(GridModel gridModel)
        {
            switch (gridModel.DataSourceType)
            {
                case DataSourceType.SQlite:
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
                var json = TextHelper.DeobfuscateString(RequestHelper.FormValue("model", string.Empty, _context));
                GridModel gridModel = System.Text.Json.JsonSerializer.Deserialize<GridModel>(json) ?? new GridModel();
                gridModel.CurrentPage = GetPageNumber(gridModel);
                gridModel.SearchInput = RequestHelper.FormValue("searchInput", string.Empty, _context);
                gridModel.SortKey = RequestHelper.FormValue("sortKey", gridModel.SortKey, _context);
                gridModel.ExportFormat = RequestHelper.FormValue("exportformat", string.Empty, _context);
                gridModel.ColumnFilter = RequestHelper.FormValueList("columnFilter", _context);
                gridModel.ParentKey = RequestHelper.FormValue("primaryKey", gridModel.ParentKey, _context);
                return gridModel;
            }
            catch
            {
                return new GridModel();
            }
        }
        private int GetPageNumber(GridModel gridModel)
        {
            switch (triggerName)
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

        private Byte[] GetResources(string type)
        {
            var resources = new string[] { };
            switch (type)
            {
                case "css":
                    resources = ["output", "gridControl"];
                    break;
                case "js":
                    resources = ["htmx", "gridControl", "draggableDialog", "viewDialog"];
                    break;
            }

            return GetResource(type, resources);
        }

        private Byte[] GetResource(string type, string[] resources)
        {
            byte[] bytes = new byte[0];

            foreach (string resource in resources)
            {
                var resourceBytes = GetResource(type, resource);
                bytes = CombineByteArrays(bytes, resourceBytes);
                bytes = CombineByteArrays(bytes, Encoding.UTF8.GetBytes(Environment.NewLine));
            }
            return bytes;
        }

        private Byte[] GetResource(string type, string resource)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"{assembly.FullName!.Split(",").First()}.Resources.{type.ToUpper()}.{resource}.{type}";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName) ?? new MemoryStream())
            using (var binaryReader = new BinaryReader(stream))
            {
                var bytes = binaryReader.ReadBytes((int)stream.Length);
                return bytes;
            }
        }

        public static byte[] CombineByteArrays(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }
    }
}