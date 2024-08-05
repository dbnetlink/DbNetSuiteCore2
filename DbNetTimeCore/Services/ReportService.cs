using DbNetTimeCore.Services.Interfaces;
using DbNetTimeCore.Repositories;
using System.Reflection;
using System.Text;
using TQ.Models;
using System.Data;
using DbNetTimeCore.Helpers;
using DbNetTimeCore.Enums;
using Microsoft.AspNetCore.StaticFiles;
using ClosedXML.Excel;
using Newtonsoft.Json;
using TQ.Components.Constants;

namespace DbNetTimeCore.Services
{
    public class ReportService : IReportService
    {
        private readonly IMSSQLRepository _msSqlRepository;
        private readonly ITimestreamRepository _timestreamRepository;
        private readonly ISQLiteRepository _sqliteRepository;
        private readonly RazorViewToStringRenderer _razorRendererService;
        private readonly IJSONRepository _jsonRepository;

        private HttpContext? _context = null;
        private string Handler => RequestHelper.QueryValue("handler", string.Empty, _context);
        private bool isAjaxCall => _context.Request.Headers["hx-request"] == "true";

        private string triggerName => _context.Request.Headers.Keys.Contains("hx-trigger-name") ? _context.Request.Headers["hx-trigger-name"] : "";

        private DataSourceType dataSourceType => Enum.Parse<DataSourceType>(RequestHelper.FormValue("dataSourceType", string.Empty, _context));

        public ReportService(IMSSQLRepository msSqlRepository, RazorViewToStringRenderer razorRendererService, ITimestreamRepository timestreamRepository, ISQLiteRepository sqliteRepository, IJSONRepository jsonRepository)
        {
            _msSqlRepository = msSqlRepository;
            _razorRendererService = razorRendererService;
            _timestreamRepository = timestreamRepository;
            _sqliteRepository = sqliteRepository;
            _jsonRepository = jsonRepository;
        }

        public async Task<Byte[]> Process(HttpContext context, string page)
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

            return new byte[0];
        }

        private async Task<Byte[]> GridView(GridModel gridModel)
        {
            if (triggerName == TriggerNames.Download)
            {
                return await ExportRecords(gridModel);
            }
            else
            {
                return await View("_gridMarkup", await GetGridViewModel(gridModel));
            }
        }

        private async Task<Byte[]> View<TModel>(string viewName, TModel model)
        {
            return Encoding.UTF8.GetBytes(await _razorRendererService.RenderViewToStringAsync($"Views/{viewName}.cshtml", model));
        }

        private async Task<GridViewModel> GetGridViewModel(GridModel gridModel)
        {
            DataTable columns = await GetColumns(gridModel);
            DataTable data = await GetRecords(gridModel);

            var unInitialisedColumns = new List<GridColumnModel>(gridModel.Columns.Where(c => c.Initialised == false));

            if (gridModel.Columns.Any() == false || unInitialisedColumns.Any())
            {
                if (gridModel.Columns.Any() == false)
                {
                    gridModel.Columns = columns.Columns.Cast<DataColumn>().Select(c => new GridColumnModel(c)).Cast<GridColumnModel>().ToList();
                }
                else
                {
                    var dataColumns = columns.Columns.Cast<DataColumn>().ToList();
                    for (var i = 0; i < dataColumns.Count; i++)
                    {
                        gridModel.Columns[i].Update(dataColumns[i]);
                    }
                }
                for (var i = 0; i < gridModel.Columns.Count; i++)
                {
                    gridModel.Columns[i].Ordinal = i + 1;
                }
            }

            return new GridViewModel(data, gridModel);
        }


        private async Task<DataTable> GetRecords(GridModel gridModel)
        {
            switch (gridModel.DataSourceType)
            {
                case DataSourceType.Timestream:
                    return await _timestreamRepository.GetRecords(gridModel);
                case DataSourceType.SQlite:
                    return await _sqliteRepository.GetRecords(gridModel);
                case DataSourceType.JSON:
                    return await _jsonRepository.GetRecords(gridModel, _context);
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
                case DataSourceType.JSON:
                    return await _jsonRepository.GetColumns(gridModel, _context);
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
            return await View("_gridExport", await GetGridViewModel(gridModel));
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
            GridModel gridModel = System.Text.Json.JsonSerializer.Deserialize<GridModel>(RequestHelper.FormValue("model", string.Empty, _context)) ?? new GridModel();
            try
            {
                gridModel.CurrentPage = (triggerName == "page") ? Convert.ToInt32(RequestHelper.FormValue("page", "1", _context)) : Convert.ToInt32(RequestHelper.QueryValue("page", "1", _context));
                gridModel.SearchInput = RequestHelper.FormValue("searchInput", string.Empty, _context);
                gridModel.SortKey = RequestHelper.FormValue("sortKey", string.Empty, _context);
                gridModel.CurrentSortKey = RequestHelper.FormValue("currentSortKey", string.Empty, _context);
                gridModel.CurrentSortAscending = Convert.ToBoolean(RequestHelper.FormValue("currentSortAscending", "false", _context));
                gridModel.ExportFormat = RequestHelper.FormValue("exportformat", string.Empty, _context);
            }
            catch
            {
                return null;
            }

            if (triggerName == TriggerNames.Search)
            {
                gridModel.CurrentPage = 1;
            }

            return gridModel;
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
                    resources = ["daisyui", "gridcontrol"];
                    break;
                case "js":
                    resources = ["tailwindcss", "htmx.min", "surreal", "gridcontrol"];
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

        private bool ValidateFilmEditForm(FormModel formModel)
        {
            return StandardFormValidation(formModel);
        }

        private bool ValidateCustomerEditForm(FormModel formModel)
        {
            return StandardFormValidation(formModel);
        }

        private bool ValidateActorEditForm(FormModel formModel)
        {
            return StandardFormValidation(formModel);
        }

        private bool StandardFormValidation(FormModel formModel)
        {
            var formValues = (FormCollection)_context.Request.Form;

            formModel.SavedFormValues = new Dictionary<string, object>(formModel.FormValues((FormCollection)formValues));

            foreach (var column in formModel.EditColumns)
            {
                if (column.Required && string.IsNullOrEmpty(formValues[column.Name]))
                {
                    formModel.Message = "Highlighted column is required";
                    column.Invalid = true;
                    break;
                }
            }

            if (formModel.EditColumns.Any(c => c.Invalid))
            {
                return false;
            }

            foreach (var column in formModel.EditColumns)
            {
                var value = formValues[column.Name].ToString();
                if (string.IsNullOrEmpty(value) || column.DataType == typeof(bool))
                {
                    continue;
                }

                try
                {
                    var convertedValue = Convert.ChangeType(value, column.DataType);
                }
                catch
                {
                    formModel.Message = "Highlighted column is not in correct format";
                    column.Invalid = true;
                    break;
                }
            }

            var inValid = formModel.EditColumns.Any(c => c.Invalid);
            return inValid == false;
        }
    }
}