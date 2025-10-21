using DbNetSuiteCore.Models;
using System.Data;
using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Helpers;
using Sylvan.Data.Excel;
using Sylvan.Data.Csv;
using Microsoft.Extensions.Caching.Memory;

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
            else
            {
                dataTable = LoadSpreadsheet(componentModel);
            }

            if (componentModel.GetColumns().Any())
            {
                string[] selectedColumns = componentModel.GetColumns().Select(c => c.Expression.Replace("[", string.Empty).Replace("]", string.Empty)).ToArray();
                dataTable = new DataView(dataTable).ToTable(false, selectedColumns);
            }

            if (componentModel.Cache)
            {
                _memoryCache.Set(componentModel.Id, dataTable, GetCacheOptions());
            }

            return dataTable;
        }

        private DataTable LoadSpreadsheet(ComponentModel componentModel)
        {
            DataTable dataTable = new DataTable();
            try
            {
                using ExcelDataReader edr = ExcelDataReader.Create(FilePath(componentModel.Url));
                {
                    dataTable.Load(edr);
                }
                foreach (ColumnModel column in componentModel.GetColumns())
                {
                    if (column.DataType != typeof(string))
                    {
                        dataTable.UpdateColumnDataType(column.Expression, column.DataType);
                    }
                }
            }
            catch (Exception)
            {
                throw new Exception($"Unable to read the Excel file {componentModel.Url}");
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

        private string FilePath(string filePath)
        {
            if (TextHelper.IsAbsolutePath(filePath))
            {
                return filePath;

            }
            return $"{_env.WebRootPath}{filePath.Replace("/", @"\")}".Replace("//", "/");
        }
    }
}