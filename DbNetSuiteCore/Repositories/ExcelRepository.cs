using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using DocumentFormat.OpenXml;
using ExcelDataReader;
using Microsoft.Extensions.Caching.Memory;
using System.Data;
using System.Text;

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
            if (componentModel.Cache && _memoryCache.TryGetValue(componentModel.CacheKey, out DataTable? dataTable))
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

            foreach (ColumnModel column in componentModel.GetColumns())
            {
                if (column.DataType != typeof(DBNull))
                {
                    dataTable.UpdateColumnDataType(column.Expression, column.DataType);
                }
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
                    _memoryCache.Set(componentModel.CacheKey, dataTable, GetCacheOptions());
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
                        using (MemoryStream ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            dataTable = GetDataTableFromStream(ms, componentModel);
                        }
                    }
                }
                else
                {
                    using (var stream = File.Open(FilePath(componentModel.Url), FileMode.Open, FileAccess.Read))
                    {
                        dataTable = GetDataTableFromStream(stream, componentModel);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to read the Excel file {componentModel.Url} - {ex.Message}");
            }

            return dataTable;
        }

        private DataTable GetDataTableFromStream(Stream stream, ComponentModel componentModel)
        {
            using (var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration
            {
                FallbackEncoding = System.Text.Encoding.GetEncoding(1252)
            }))
            {
                new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = true
                    }
                };
                DataSet dataSet = reader.AsDataSet(DataSetReaderConfiguration());
                DataTable dataTable = dataSet.Tables[0];
                if (componentModel is GridModel gridModel)
                {
                    dataTable = dataSet.GetTable(gridModel.SheetName);
                }

                return dataTable;
            }
        }

        private DataTable CsvToDataTable(ComponentModel componentModel)
        {
            if (Uri.IsWellFormedUriString(componentModel.Url, UriKind.Absolute))
            {
                using (HttpClient client = new HttpClient())
                using (Stream stream = client.GetStreamAsync(componentModel.Url).Result)
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    ms.Position = 0;
                    return CsvStreamToDataTable(ms);
                }
            }
            else
            {
                using (var stream = File.Open(FilePath(componentModel.Url), FileMode.Open, FileAccess.Read))
                return CsvStreamToDataTable(stream);
            }
        }

        private DataTable CsvStreamToDataTable(Stream stream)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Step 3: Create the CsvReader from the stream
            using (var reader = ExcelReaderFactory.CreateCsvReader(stream, new ExcelReaderConfiguration()
            {
                // Default: cp1252 (Good fallback for older CSVs)
                FallbackEncoding = Encoding.GetEncoding(1252),

                // Optional: specify delimiter candidates if the CSV might use separators other than comma
                // AutodetectSeparators = new char[] { ',', ';', '\t', '|', '#' } 
            }))
            {
                // Step 4: Convert the IExcelDataReader to a DataSet
                var result = reader.AsDataSet(DataSetReaderConfiguration());

                return result.Tables[0];
            }
        }

        private ExcelDataSetConfiguration DataSetReaderConfiguration()
        {
            return new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                {
                    // Use the first row of the CSV as the column names in the DataTable
                    UseHeaderRow = true
                }
            };
        }

        private DataTable OdsToDataTable(ComponentModel componentModel)
        {
            DataTable dataTable = new DataTable();

            using (OdsReader cdr = new OdsReader())
            {
                string? sheetName = componentModel is GridModel gridModel ? gridModel.SheetName : null;
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
            if (TextHelper.IsAbsolutePath(filePath) || filePath.ToLower().StartsWith("https://"))
            {
                return filePath;

            }
            return $"{_env.WebRootPath}{filePath.Replace("/", @"\")}".Replace("//", "/");
        }
    }
}