using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using DocumentFormat.OpenXml;
using DuckDB.NET.Data;
using Microsoft.Extensions.Caching.Memory;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public class ExcelRepository :  IExcelRepository
    {
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _memoryCache;

        public ExcelRepository(IWebHostEnvironment env, IMemoryCache memoryCache)
        {
            _env = env;
            _memoryCache = memoryCache;
        }

        public Task GetRecords(ComponentModel componentModel)
        {
            if (componentModel is GridSelectModel gridSelectModel)
            {
                var dataTable = componentModel.Data.Columns.Count > 0 ? componentModel.Data : BuildDataTable(gridSelectModel);
                if (componentModel is GridModel gridModel)
                {
                    dataTable.FilterAndSort(gridModel);
                    gridModel.ConvertEnumLookups();
                    gridModel.GetDistinctLookups();
                }

                if (componentModel is SelectModel selectModel)
                {
                    if (selectModel.Distinct)
                    {
                        var columnNames = dataTable.Columns.Cast<DataColumn>().Select(dc => dc.ColumnName).ToArray();
                        dataTable = dataTable.DefaultView.ToTable(true, columnNames);
                    }
                    dataTable.FilterAndSort(selectModel);
                    selectModel.ConvertEnumLookups();
                }
            }
            return Task.CompletedTask;
        }

        public void GetRecord(ComponentModel componentModel)
        {
            if (componentModel is GridSelectModel gridSelectModel)
            {
                var dataTable = BuildDataTable(gridSelectModel);
                dataTable.FilterWithPrimaryKey(gridSelectModel);
                gridSelectModel.ConvertEnumLookups();
            }
        }

        public Task<DataTable> GetColumns(ComponentModel componentModel)
        {
            if (componentModel is GridSelectModel gridSelectModel)
            {
                return Task.FromResult(BuildDataTable(gridSelectModel));
            }
            return Task.FromResult(new DataTable());
        }

        private DataTable BuildDataTable(GridSelectModel gridSelectModel)
        {
            DataTable dataTable = null;
                if (gridSelectModel.Cache && _memoryCache.TryGetValue(gridSelectModel.CacheKey, out dataTable))
                {
                    if (dataTable != null)
                    {
                        return dataTable;
                    }
                }

            if (ComponentModelExtensions.IsCsvFile(gridSelectModel))
            {
                dataTable = LoadSpreadsheet(gridSelectModel, FileFormat.CSV);
            }
            else if (ComponentModelExtensions.IsOdsFile(gridSelectModel))
            {
                dataTable = LoadSpreadsheet(gridSelectModel, FileFormat.ODS);
               // dataTable = LoadSpreadsheet(componentModel);
            }
            else
            {
                dataTable = LoadSpreadsheet(gridSelectModel, FileFormat.XLSX    );
            }

            foreach (ColumnModel column in gridSelectModel.GetColumns())
            {
                if (column.DataType != typeof(DBNull))
                {
           //         dataTable.UpdateColumnDataType(column.Expression, column.DataType);
                }
            }

            if (gridSelectModel.GetColumns().Any())
            {
                string[] selectedColumns = gridSelectModel.GetColumns().Select(c => c.Expression.Replace("[", string.Empty).Replace("]", string.Empty)).ToArray();
                dataTable = new DataView(dataTable).ToTable(false, selectedColumns);
            }

            if (gridSelectModel is GridModel gridModel && gridModel.Uninitialised == false)
            {
                dataTable.AcceptChanges();
                gridModel.Data = dataTable;
                PluginHelper.InvokeMethod(gridModel.CustomisationPluginName, nameof(ICustomGridPlugin.TransformDataTable), gridModel);

                if (gridModel.Cache)
                {
                    _memoryCache.Set(gridModel.CacheKey, dataTable, CacheHelper.GetCacheOptions());
                }
            }
            return dataTable;
        }

        private DataTable LoadSpreadsheet(ComponentModel componentModel, FileFormat fileFormat )
        {
            DataTable dataTable = new DataTable();
            try
            {
                dataTable = GetDataTableFromUrl(componentModel, fileFormat);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to read the file {componentModel.Url} - {ex.Message}");
            }

            return dataTable;
        }
        private DataTable GetDataTableFromUrl(ComponentModel componentModel, FileFormat fileFormat)
        {
            DataTable dataTable = new DataTable();
            using (var connection = new DuckDBConnection("DataSource=:memory:"))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = $"CREATE TABLE superstore AS FROM read_{fileFormat.ToString().ToLower()}('{FilePath(componentModel.Url)}', header = true)";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = $"SELECT * FROM superstore";
                    using var reader = cmd.ExecuteReader();
                    dataTable.Load(reader);
                    reader.DisposeAsync().ConfigureAwait(false);
                }

                connection.Close();
            }

            return dataTable;
        }


        private DataTable OdsToDataTable(ComponentModel componentModel)
        {
            DataTable dataTable = new DataTable();

            using (OdsReader cdr = new OdsReader())
            {
                string sheetName = componentModel is GridModel gridModel ? gridModel.SheetName : null;
                if (Uri.IsWellFormedUriString(componentModel.Url, UriKind.Absolute))
                {
                    dataTable = cdr.GetDataTableFromUrl(componentModel.Url, sheetName);
                }
                else
                {
                    dataTable = cdr.GetDataTableFromPath(FilePath(componentModel.Url), sheetName);
                }

                return dataTable;
            }
        }

        private string FilePath(string filePath)
        {
            if (TextHelper.IsAbsolutePath(filePath) || Uri.IsWellFormedUriString(filePath, UriKind.Absolute))
            {
                return filePath;
            }
            return $"{_env.WebRootPath}{filePath.Replace("/", @"\")}".Replace("//", "/");
        }
    }
}