using ClosedXML.Excel;
using DbNetSuiteCore.Constants;
using DbNetSuiteCore.CustomisationHelpers.Interfaces;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Middleware;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Services.Interfaces;
using DbNetSuiteCore.ViewModels;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.Word;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Data;
using System.Text;

namespace DbNetSuiteCore.Services
{
    public class GridService : ComponentService, IComponentService
    {
        public GridService(IMSSQLRepository msSqlRepository, RazorViewToStringRenderer razorRendererService, ISQLiteRepository sqliteRepository, IJSONRepository jsonRepository, IFileSystemRepository fileSystemRepository, IMySqlRepository mySqlRepository, IPostgreSqlRepository postgreSqlRepository, IExcelRepository excelRepository, IMongoDbRepository mongoDbRepository, IOracleRepository oracleRepository, IConfiguration configuration, IWebHostEnvironment webHostEnvironment) : base(msSqlRepository, razorRendererService, sqliteRepository, jsonRepository, fileSystemRepository, mySqlRepository, postgreSqlRepository, excelRepository, mongoDbRepository, oracleRepository, configuration, webHostEnvironment)
        {
        }

        public async Task<Byte[]> Process(HttpContext context, string page)
        {
            if (context.Request.Method != "POST")
            {
                return new byte[0];
            }

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

            CheckLicense(gridModel);

            switch (gridModel.TriggerName)
            {
                case TriggerNames.Download:
                    return await ExportRecords(gridModel);
                case TriggerNames.Apply:
                    return await ApplyUpdate(gridModel);
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
            await GetRecord(gridModel);
            return new GridViewDialogViewModel(gridModel);
        }

        private async Task<GridViewModel> GetGridViewModel(GridModel gridModel)
        {
            if (gridModel.DataSourceType == DataSourceType.FileSystem && string.IsNullOrEmpty(gridModel.ParentModel?.Name) == false)
            {
                FileSystemRepository.UpdateUrl(gridModel);
            }

            if (gridModel.IsStoredProcedure == false && gridModel.Uninitialised)
            {
                await ConfigureColumns(gridModel);

                if (gridModel.IsEditable && gridModel.Columns.Any(c => c.PrimaryKey) == false)
                {
                    throw new Exception("At least one grid column must be designated as a primary key for it to be editable");
                }

                foreach (var column in gridModel.Columns.Where(c => c.Editable))
                {
                    column.FormColumn = new FormColumn(column.Expression);
                    ColumnsHelper.CopyPropertiesTo(column, column.FormColumn);
                }

                if (string.IsNullOrEmpty(gridModel.CustomisationPluginName) == false)
                {
                    PluginHelper.InvokeMethod(gridModel.CustomisationPluginName, nameof(ICustomGridPlugin.Initialisation), new object[] { gridModel, _context, _configuration });
                }
            }

            await GetGridRecords(gridModel);
            if (gridModel.IsStoredProcedure && gridModel.Uninitialised)
            {
                ConfigureColumnsForStoredProcedure(gridModel);
            }

            ConfigureFormColumns(gridModel);

            if (gridModel.IncludeJsonData)
            {
                gridModel.JsonData = JsonConvert.SerializeObject(gridModel.Data);
            }

            gridModel.CurrentSortKey = RequestHelper.FormValue("sortKey", gridModel.CurrentSortKey, _context);
            gridModel.FormValues.Clear();
            gridModel.Columns.ToList().ForEach(c => c.LineInError.Clear());

            if (gridModel.SummaryModel == null)
            {
                gridModel.SummaryModel = new SummaryModel(gridModel);
            }

            var gridViewModel = new GridViewModel(gridModel);

            if (gridModel.DiagnosticsMode)
            {
                gridViewModel.Diagnostics = RequestHelper.Diagnostics(_context, _configuration, _webHostEnvironment);
            }
           
            return gridViewModel;
        }

        private List<GridModel> ConfigureNestedGrid(GridModel gridModel)
        {
            foreach (var nestedGrid in gridModel._NestedGrids)
            {
                nestedGrid.IsNested = true;
                nestedGrid.Caption = string.Empty;
                //nestedGrid.ParentKey = RequestHelper.FormValue("primaryKey", "", _context);
                nestedGrid.AssignParentModel(_context, _configuration, "summarymodel");
                nestedGrid.SetId();

                if (gridModel.DataSourceType == DataSourceType.FileSystem)
                {
                    var nestedChildGrid = gridModel._NestedGrids.First().DeepCopy();
                    nestedChildGrid.Url = $"{nestedChildGrid.Url}/{nestedGrid.ParentModel!.Name}";
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
                case TriggerNames.ApiRequestParameters:
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
                gridModel.JSON = TextHelper.Decompress(RequestHelper.FormValue("json", string.Empty, _context) ?? string.Empty);
                gridModel.CurrentPage = gridModel.ToolbarPosition == ToolbarPosition.Hidden ? 1 : GetPageNumber(gridModel);
                gridModel.SearchInput = RequestHelper.FormValue("searchInput", string.Empty, _context)?.Trim() ?? string.Empty;
                gridModel.SortKey = RequestHelper.FormValue("sortKey", gridModel.SortKey, _context) ?? string.Empty;
                gridModel.ExportFormat = RequestHelper.FormValue("exportformat", string.Empty, _context) ?? string.Empty;
                gridModel.ColumnFilter = RequestHelper.FormValueList("columnFilter", _context).Select(f => f.Trim()).ToList();
                gridModel.FormValues = RequestHelper.GridFormColumnValues(_context, gridModel);
                gridModel.SearchDialogConjunction = RequestHelper.FormValue("searchDialogConjunction", "and", _context)?.Trim() ?? string.Empty;
                gridModel.RowsModified = RequestHelper.GetModifiedRows(_context, gridModel);
                gridModel.ValidationPassed = ComponentModelExtensions.ParseBoolean(RequestHelper.FormValue("validationPassed", gridModel.ValidationPassed.ToString(), _context) ?? "false");
                gridModel.RowId = RequestHelper.FormValue(TriggerNames.ViewDialogContent, string.Empty, _context) ?? string.Empty;

                if (gridModel.DataSourceType == DataSourceType.JSON)
                { 
                   _jsonRepository.UpdateApiRequestParameters(gridModel, _context);
                }

                AssignParentModel(gridModel);
                AssignSearchDialogFilter(gridModel);

                gridModel.Columns.ToList().ForEach(column => column.FilterError = string.Empty);
                gridModel.Message = string.Empty;

                return gridModel;
            }
            catch(Exception ex)
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
                case TriggerNames.Cancel:
                case TriggerNames.Apply:
                    return Convert.ToInt32(RequestHelper.FormValue("page", "1", _context));
                case TriggerNames.Search:
                case TriggerNames.First:
                case TriggerNames.ColumnFilter:
                case TriggerNames.SearchDialog:
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

        private void ConfigureFormColumns(GridModel gridModel)
        {
            foreach (var column in gridModel.Columns.Where(c => c.Editable))
            {
                column.FormColumn.SetLookupOptions(column);
            }
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

        private async Task<Byte[]> ApplyUpdate(GridModel gridModel)
        {
            if (gridModel.ClientEvents.Keys.Contains(GridClientEvent.ValidateUpdate) == false)
            {
                if (ValidateRecord(gridModel))
                {
                    await CommitUpdate(gridModel);
                }
            }
            else if (gridModel.ValidationPassed == false)
            {
                gridModel.ValidationPassed = ValidateRecord(gridModel);
            }
            else
            {
                await CommitUpdate(gridModel);
            }

            await GetGridRecords(gridModel);
            GridViewModel gridViewModel = new GridViewModel(gridModel);
            return await View("Grid/__Rows", gridViewModel);
        }

        private async Task CommitUpdate(GridModel gridModel)
        {
            await UpdateRecords(gridModel);
            gridModel.FormValues = new Dictionary<string, List<string>>();
            gridModel.Message = ResourceHelper.GetResourceString(ResourceNames.Updated);
            gridModel.MessageType = MessageType.Success;
        }

        protected async Task UpdateRecords(GridModel gridModel)
        {
            switch (gridModel.DataSourceType)
            {
                case DataSourceType.SQLite:
                    await _sqliteRepository.UpdateRecords(gridModel);
                    break;
                case DataSourceType.MySql:
                    await _mySqlRepository.UpdateRecords(gridModel);
                    break;
                case DataSourceType.PostgreSql:
                    await _postgreSqlRepository.UpdateRecords(gridModel);
                    break;
                case DataSourceType.MongoDB:
                    await _mongoDbRepository.UpdateRecords(gridModel);
                    break;
                case DataSourceType.Oracle:
                    await _oracleRepository.UpdateRecords(gridModel);
                    break;
                default:
                    await _msSqlRepository.UpdateRecords(gridModel);
                    break;
            }
        }

        private bool ValidateRecord(GridModel gridModel)
        {
            var validationTypes = new List<ResourceNames>() { ResourceNames.Required, ResourceNames.DataFormatError, ResourceNames.MinCharsError, ResourceNames.MaxCharsError, ResourceNames.MinValueError, ResourceNames.PatternError };

            foreach (var validationType in validationTypes)
            {
                if (ValidateErrorType(gridModel, validationType) == false)
                {
                    return false;
                }
            }

            bool result = true;

            if (String.IsNullOrWhiteSpace(gridModel.CustomisationPluginName) == false)
            {
                result = (bool)PluginHelper.InvokeMethod(gridModel.CustomisationPluginName, nameof(ICustomGridPlugin.ValidateUpdate), new object[] { gridModel, _context, _configuration })!;

                if (result == false)
                {
                    if (string.IsNullOrEmpty(gridModel.Message))
                    {
                        gridModel.Message = "Custom validation failed";
                    }
                    gridModel.MessageType = MessageType.Error;
                }
            }

            return result;
        }

        private bool ValidateErrorType(GridModel gridModel, ResourceNames resourceName)
        {
            int rows = gridModel.FormValues[gridModel.FirstEditableColumnName].Count;

            foreach (GridColumn? gridColumn in gridModel.Columns.Where(c => c.Editable))
            {
                gridColumn.LineInError = new List<bool>();
                for (var r = 0; r < rows; r++)
                {
                    gridColumn.LineInError.Add(false);
                }
            }

            for (var r = 0; r < rows; r++)
            {
                foreach (GridColumn? gridColumn in gridModel.Columns.Where(c => c.Editable))
                {
                    var columnName = gridColumn.ColumnName;

                    var value = string.Empty;

                    if (gridModel.FormValues.ContainsKey(columnName))
                    {
                        value = gridModel.FormValues[columnName][r];
                    }

                    gridColumn.InError = false;
                    ValidateFormValue(gridColumn, value, resourceName, gridModel);
                    gridColumn.LineInError[r] = gridColumn.InError;

                    if (CellSpecificError())
                    {
                        break;
                    }
                }

                if (CellSpecificError())
                {
                    break;
                }
            }

            if (gridModel.Columns.Any(c => c.LineInError.Contains(true)))
            {
                if (string.IsNullOrEmpty(gridModel.Message))
                {
                    gridModel.Message = ResourceHelper.GetResourceString(resourceName);
                }
                gridModel.MessageType = MessageType.Error;
                return false;
            }

            return true;

            bool CellSpecificError()
            {
                return resourceName == ResourceNames.MinValueError && gridModel.Columns.Any(c => c.InError);
            }
        }
    }
}