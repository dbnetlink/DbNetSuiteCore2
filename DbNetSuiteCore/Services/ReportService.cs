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

namespace DbNetSuiteCore.Services
{
    public class ReportService : IReportService
    {
        private readonly IMSSQLRepository _msSqlRepository;
        private readonly ITimestreamRepository _timestreamRepository;
        private readonly ISQLiteRepository _sqliteRepository;
        private readonly RazorViewToStringRenderer _razorRendererService;
        private readonly IJSONRepository _jsonRepository;
        private readonly IFileSystemRepository _fileSystemRepository;
        private readonly IMySqlRepository _mySqlRepository;
        private readonly IPostgreSqlRepository _postgreSqlRepository;

        private HttpContext? _context = null;
        private string Handler => RequestHelper.QueryValue("handler", string.Empty, _context);
        private bool isAjaxCall => _context.Request.Headers["hx-request"] == "true";

        private string triggerName => _context.Request.Headers.Keys.Contains("hx-trigger-name") ? _context.Request.Headers["hx-trigger-name"] : "";

        private DataSourceType dataSourceType => Enum.Parse<DataSourceType>(RequestHelper.FormValue("dataSourceType", string.Empty, _context));

        public ReportService(IMSSQLRepository msSqlRepository, RazorViewToStringRenderer razorRendererService, ITimestreamRepository timestreamRepository, ISQLiteRepository sqliteRepository, IJSONRepository jsonRepository, IFileSystemRepository fileSystemRepository, IMySqlRepository mySqlRepository, IPostgreSqlRepository postgreSqlRepository)
        {
            _msSqlRepository = msSqlRepository;
            _razorRendererService = razorRendererService;
            _timestreamRepository = timestreamRepository;
            _sqliteRepository = sqliteRepository;
            _jsonRepository = jsonRepository;
            _fileSystemRepository = fileSystemRepository;
            _mySqlRepository = mySqlRepository;
            _postgreSqlRepository = postgreSqlRepository;
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
                        GridModel gridModel = GetGridModel() ?? new GridModel();
                        return await GridView(gridModel);
                    default:
                        return GetResource(page.Split(".").Last(), page.Split(".").First());
                }
            }
            catch (Exception ex)
            {
                return await View("Error", ex);
            }
        }

        private async Task<Byte[]> GridView(GridModel gridModel)
        {
            switch (triggerName)
            {
                case TriggerNames.Download:
                    return await ExportRecords(gridModel);
                case TriggerNames.NestedGrid:
                    gridModel.NestedGrid!.IsNested = true;
                    gridModel.NestedGrid!.ColSpan = gridModel.Columns.Count;
                    gridModel.NestedGrid!.ParentKey = RequestHelper.FormValue("primaryKey","",_context);
                    gridModel.NestedGrid.SetId();

                    if (gridModel.DataSourceType == DataSourceType.FileSystem)
                    {
                        gridModel.NestedGrid.NestedGrid = gridModel.NestedGrid.DeepCopy();
                    }
                    return await View("NestedGrid", gridModel.NestedGrid);
                default:
                    string viewName = gridModel.Uninitialised ? "GridMarkup" : "GridRows";
                    return await View(viewName, await GetGridViewModel(gridModel));
            }
        }

        private async Task<Byte[]> View<TModel>(string viewName, TModel model)
        {
            return Encoding.UTF8.GetBytes(await _razorRendererService.RenderViewToStringAsync($"Views/{viewName}.cshtml", model));
        }

        private async Task<GridViewModel> GetGridViewModel(GridModel gridModel)
        {
            if (gridModel.DataSourceType == DataSourceType.FileSystem && string.IsNullOrEmpty(gridModel.ParentKey) == false)
            {
                FileSystemRepository.UpdateUrl(gridModel);
            }
            await ConfigureGridColumns(gridModel);
            DataTable data = await GetRecords(gridModel);
            return new GridViewModel(data, gridModel);
        }

        private async Task ConfigureGridColumns(GridModel gridModel)
        {
            if (gridModel.Uninitialised)
            {
                DataTable schema = await GetColumns(gridModel);

                if (gridModel.Columns.Any() == false)
                {
                    switch (gridModel.DataSourceType)
                    {
                        case DataSourceType.MSSQL:
                            gridModel.Columns = schema.Rows.Cast<DataRow>().Select(r => new GridColumnModel(r, gridModel.DataSourceType)).Cast<GridColumnModel>().Where(c => c.Valid).ToList();
                            break;
                        default:
                            gridModel.Columns = schema.Columns.Cast<DataColumn>().Select(c => new GridColumnModel(c)).Cast<GridColumnModel>().ToList();
                            break;
                    }

                    gridModel.QualifyColumnExpressions();
                }
                else
                {
                    switch (gridModel.DataSourceType)
                    {
                        case DataSourceType.MSSQL:
                            gridModel.Columns = schema.Rows.Cast<DataRow>().Select(r => new GridColumnModel(r, gridModel.DataSourceType)).Cast<GridColumnModel>().ToList();
                            var dataRows = schema.Rows.Cast<DataRow>().ToList();
                            for (var i = 0; i < dataRows.Count; i++)
                            {
                                gridModel.Columns[i].Update(dataRows[i]);
                            }
                            break;
                        default:
                            var dataColumns = schema.Columns.Cast<DataColumn>().ToList();

                            if (gridModel.DataSourceType == DataSourceType.FileSystem)
                            {
                                foreach (GridColumnModel gridColumn in gridModel.Columns)
                                {
                                    gridColumn.Update(dataColumns.First(dc => dc.ColumnName == gridColumn.Expression));
                                }
                            }
                            else
                            {
                                for (var i = 0; i < dataColumns.Count; i++)
                                {
                                    gridModel.Columns[i].Update(dataColumns[i]);
                                }
                            }
                            break;
                    }
                }
                for (var i = 0; i < gridModel.Columns.Count; i++)
                {
                    gridModel.Columns[i].Ordinal = i + 1;
                }
            }
        }

        private async Task<DataTable> GetRecords(GridModel gridModel)
        {
            switch (gridModel.DataSourceType)
            {
                case DataSourceType.Timestream:
                    return await _timestreamRepository.GetRecords(gridModel);
                case DataSourceType.SQlite:
                    return await _sqliteRepository.GetRecords(gridModel);
                case DataSourceType.MySql:
                    return await _mySqlRepository.GetRecords(gridModel);
                case DataSourceType.PostgreSql:
                    return await _postgreSqlRepository.GetRecords(gridModel);
                case DataSourceType.JSON:
                    return await _jsonRepository.GetRecords(gridModel, _context);
                case DataSourceType.FileSystem:
                    return await _fileSystemRepository.GetRecords(gridModel, _context);
                default:
                    return await _msSqlRepository.GetRecords(gridModel);
            }
        }

        private async Task<DataTable> GetColumns(GridModel gridModel)
        {
            switch (gridModel.DataSourceType)
            {
                case DataSourceType.Timestream:
                    return await _timestreamRepository.GetColumns(gridModel);
                case DataSourceType.SQlite:
                    return await _sqliteRepository.GetColumns(gridModel);
                case DataSourceType.MySql:
                    return await _mySqlRepository.GetColumns(gridModel);
                case DataSourceType.PostgreSql:
                    return await _postgreSqlRepository.GetColumns(gridModel);
                case DataSourceType.JSON:
                    return await _jsonRepository.GetColumns(gridModel, _context);
                case DataSourceType.FileSystem:
                    return await _fileSystemRepository.GetColumns(gridModel, _context);
                default:
                    return await _msSqlRepository.GetColumns(gridModel);
            }
        }

        private async Task<Byte[]> ExportRecords(GridModel gridModel)
        {
            DataTable data = await GetRecords(gridModel);
            switch (gridModel.ExportFormat)
            {
                case "csv":
                    return ConvertDataTableToCSV(data);
                case "excel":
                    return ConvertDataTableToSpreadsheet(data, gridModel);
                case "json":
                    return ConvertDataTableToJSON(data);
                default:
                    return await ConvertDataTableToHTML(data, gridModel);
            }
        }

        private Byte[] ConvertDataTableToCSV(DataTable dt)
        {
            StringBuilder sb = new StringBuilder();

            IEnumerable<string> columnNames = dt.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in dt.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field => string.Concat("\"", field.ToString().Replace("\"", "\"\""), "\""));
                sb.AppendLine(string.Join(",", fields));
            }
            _context.Response.ContentType = GetMimeTypeForFileExtension(".csv");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private async Task<Byte[]> ConvertDataTableToHTML(DataTable dt, GridModel gridModel)
        {
            gridModel.PageSize = dt.Rows.Count;
            _context.Response.ContentType = GetMimeTypeForFileExtension(".html");
            return await View("GridExport", await GetGridViewModel(gridModel));
        }

        private byte[] ConvertDataTableToJSON(DataTable dataTable)
        {
            var json = JsonConvert.SerializeObject(dataTable, Newtonsoft.Json.Formatting.Indented);
            _context.Response.ContentType = GetMimeTypeForFileExtension(".json");
            return Encoding.UTF8.GetBytes(json);
        }

        private byte[] ConvertDataTableToSpreadsheet(DataTable dataTable, GridModel gridModel)
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

                foreach (DataRow row in dataTable.Rows)
                {
                    rowIdx++;
                    colIdx = 1;

                    foreach (var column in gridModel.Columns)
                    {

                        object value = row[dataTable.Columns[colIdx - 1]];

                        if (value == null || value == DBNull.Value)
                        {
                            worksheet.Cell(rowIdx, colIdx).Value = string.Empty;
                        }
                        else
                        {

                            var cell = worksheet.Cell(rowIdx, colIdx);

                            switch (column.DataTypeName)
                            {
                                case nameof(Double):
                                    cell.Value = Convert.ToDouble(value);
                                    break;
                                case nameof(Decimal):
                                    cell.Value = Convert.ToDecimal(value);
                                    break;
                                case nameof(Int16):
                                case nameof(Int32):
                                case nameof(Int64):
                                    cell.Value = Convert.ToInt64(value);
                                    break;
                                case nameof(DateTime):
                                    cell.Value = Convert.ToDateTime(value);
                                    break;
                                case nameof(TimeSpan):
                                    cell.Value = TimeSpan.Parse(value?.ToString() ?? string.Empty);
                                    break;
                                case nameof(System.Boolean):
                                    cell.Value = Convert.ToBoolean(value);
                                    break;
                                default:
                                    cell.Value = value.ToString();
                                    break;
                            }

                            if (columnWidths.ContainsKey(column.ColumnName))
                            {
                                if (value.ToString().Length > columnWidths[column.ColumnName])
                                {
                                    columnWidths[column.ColumnName] = value.ToString().Length;
                                }
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
                    _context.Response.ContentType = GetMimeTypeForFileExtension($".xlsx"); ;
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    return memoryStream.ToArray();
                }
            }
        }

        private GridModel? GetGridModel()
        {
            GridModel gridModel = System.Text.Json.JsonSerializer.Deserialize<GridModel>(TextHelper.DeobfuscateString(RequestHelper.FormValue("model", string.Empty, _context))) ?? new GridModel();
            try
            {
                gridModel.CurrentPage = GetPageNumber(gridModel);
                gridModel.SearchInput = RequestHelper.FormValue("searchInput", string.Empty, _context);
                gridModel.SortKey = RequestHelper.FormValue("sortKey", string.Empty, _context);
                gridModel.CurrentSortKey = RequestHelper.FormValue("currentSortKey", string.Empty, _context);
                gridModel.CurrentSortAscending = Convert.ToBoolean(RequestHelper.FormValue("currentSortAscending", "false", _context));
                gridModel.ExportFormat = RequestHelper.FormValue("exportformat", string.Empty, _context);
                gridModel.ColumnFilter = RequestHelper.FormValueList("columnFilter", _context);
                string primaryKey = RequestHelper.FormValue("primaryKey", string.Empty, _context);
                if (!string.IsNullOrEmpty(primaryKey))
                {
                    gridModel.ParentKey = primaryKey;
                }
                
            }
            catch
            {
                return null;
            }

            return gridModel;
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
                    resources = ["output", "gridcontrol"];
                    break;
                case "js":
                    resources = ["htmx", "gridcontrol"];
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