using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using DocumentFormat.OpenXml;
using ExcelDataReader;
using Microsoft.Extensions.Caching.Memory;
using Sylvan.Data.Csv;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public class ExcelRepository : FileRepository, IExcelRepository
    {
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _memoryCache;

        public ExcelRepository(IWebHostEnvironment env, IMemoryCache memoryCache)
        {
            _env = env;
            _memoryCache = memoryCache;
        }
        public void GetRecords(ComponentModel componentModel)
        {
            var dataTable = componentModel.Data.Columns.Count > 0 ? componentModel.Data : BuildDataTable(componentModel);
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

        public void GetRecord(ComponentModel componentModel)
        {
            var dataTable = BuildDataTable(componentModel);
            dataTable.FilterWithPrimaryKey(componentModel);
            componentModel.ConvertEnumLookups();
        }

        public DataTable GetColumns(ComponentModel componentModel)
        {
            return BuildDataTable(componentModel);
        }

        private DataTable BuildDataTable(ComponentModel componentModel)
        {
            if (componentModel.Cache && _memoryCache.TryGetValue(componentModel.Id, out DataTable? dataTable))
            {
                if (dataTable != null)
                {
                    return dataTable;
                }
            }

            if (ComponentModelExtensions.IsCsvFile(componentModel))
            {
                dataTable = CsvToDataTable(componentModel);
            }
            else if (ComponentModelExtensions.IsOdsFile(componentModel))
            {
                dataTable = OdsToDataTable(componentModel);
            }
            else
            {
                dataTable = LoadSpreadsheet(componentModel);
            }

            if (componentModel.GetColumns().Any())
            {
                string[] selectedColumns = componentModel.GetColumns().Select(c => c.Expression.Replace("[", string.Empty).Replace("]", string.Empty)).ToArray();
                dataTable = new DataView(dataTable).ToTable(false, selectedColumns);
            }

            if (componentModel is GridModel gridModel && gridModel.Uninitialised == false)
            {
                dataTable.AcceptChanges();
                gridModel.Data = dataTable;
                PluginHelper.InvokeMethod(gridModel.CustomisationPluginName, nameof(ICustomGridPlugin.TransformDataTable), gridModel);

                if (componentModel.Cache)
                {
                    _memoryCache.Set(componentModel.Id, dataTable, GetCacheOptions());
                }
            }
            return dataTable;
        }

        private DataTable LoadSpreadsheet(ComponentModel componentModel)
        {
            DataTable dataTable = new DataTable();
            try
            {
                if (Uri.IsWellFormedUriString(componentModel.Url, UriKind.Absolute))
                {
                    using (HttpClient client = new HttpClient())
                    using (Stream stream = client.GetStreamAsync(componentModel.Url).Result)
                    {
                        using (MemoryStream _ms = new MemoryStream())
                        {
                            stream.CopyTo(_ms);
                            using (IExcelDataReader edr = ExcelReaderFactory.CreateReader(_ms))
                            {
                                DataSet dataSet = edr.AsDataSet();
                                dataTable = dataSet.Tables[0];
                                if (componentModel is GridModel gridModel)
                                {
                                    dataTable = dataSet.GetTable(gridModel.SheetName);
                                }

                                dataTable.Load(edr);
                            }
                        }
                    }
                }
                else
                {
                    using (var stream = File.Open(FilePath(componentModel.Url), FileMode.Open, FileAccess.Read))
                    using (IExcelDataReader edr = ExcelReaderFactory.CreateReader(stream))
                    {
                        dataTable.Load(edr);
                    }
                }
                foreach (ColumnModel column in componentModel.GetColumns())
                {
                    if (column.DataType != typeof(string))
                    {
                        dataTable.UpdateColumnDataType(column.Expression, column.DataType);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to read the Excel file {componentModel.Url} - {ex.Message}");
            }

            return dataTable;
        }

        private DataTable CsvToDataTable(ComponentModel componentModel)
        {
            DataTable dataTable = new DataTable();
            using CsvDataReader cdr = CsvDataReader.Create(FilePath(componentModel.Url));
            {
                dataTable.Load(cdr);
            }

            foreach (ColumnModel column in componentModel.GetColumns())
            {
                if (column.DataType != typeof(string))
                {
                    dataTable.UpdateColumnDataType(column.Expression, column.DataType);
                }
            }

            return dataTable;
        }

        private DataTable OdsToDataTable(ComponentModel componentModel)
        {
            DataTable dataTable = new DataTable();
            if (Uri.IsWellFormedUriString(componentModel.Url, UriKind.Absolute))
            {
                using (OdsReader cdr = new OdsReader())
                {
                    string? sheetName = componentModel is GridModel gridModel ? gridModel.SheetName : null;
                    dataTable = cdr.GetDataTableFromUrl(componentModel.Url, sheetName);
                }
            }
            else
            {
                using (OdsReader cdr = new OdsReader())
                {
                    string? sheetName = componentModel is GridModel gridModel ? gridModel.SheetName : null;
                    dataTable = cdr.GetDataTableFromPath(FilePath(componentModel.Url), sheetName);
                }


            }
            foreach (ColumnModel column in componentModel.GetColumns())
            {
                if (column.DataType != typeof(string))
                {
                    dataTable.UpdateColumnDataType(column.Expression, column.DataType);
                }
            }

            return dataTable;
        }

        private string FilePath(string filePath)
        {
            if (TextHelper.IsAbsolutePath(filePath) || filePath.ToLower().StartsWith("https://"))
            {
                return filePath;

            }
            return $"{_env.WebRootPath}{filePath.Replace("/", @"\")}".Replace("//", "/");
        }
    }
}