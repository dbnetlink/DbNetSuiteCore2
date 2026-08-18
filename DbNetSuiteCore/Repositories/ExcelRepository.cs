
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{
    public class ExcelRepository : DuckDbInMemoryRepository, IExcelRepository
    {
        public ExcelRepository(IConfiguration configuration, IWebHostEnvironment env) : base(configuration, env, Enums.DataSourceType.Excel)
        {
           
        }

        public override async Task CreateTable(ComponentModel componentModel, IDbConnection connection)
        {
            FileFormat fileFormat = FileFormat.XLSX;
            if (ComponentModelExtensions.IsCsvFile(componentModel))
            {
                fileFormat = FileFormat.CSV;
            }
            else if (ComponentModelExtensions.IsOdsFile(componentModel))
            {
                fileFormat = FileFormat.ODS;
            }

            if (fileFormat == FileFormat.ODS)
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"INSTALL rusty_sheet FROM community;LOAD rusty_sheet;";
                    cmd.ExecuteNonQuery();
                }
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"CREATE TABLE {componentModel.TableName} AS FROM ";
                if (fileFormat == FileFormat.ODS)
                {
                    cmd.CommandText += $"read_sheet('{FilePath(componentModel.Url)}', header = true";
                }
                else
                {
                    cmd.CommandText += $"read_{fileFormat.ToString().ToLower()}('{FilePath(componentModel.Url)}', header = true, normalize_names = true";
                }
                if (componentModel is GridModel gridModel)
                {
                    if (string.IsNullOrEmpty(gridModel.SheetName) == false)
                    {
                        cmd.CommandText += $", sheet='{gridModel.SheetName}'";
                    }
                    if (string.IsNullOrEmpty(gridModel.SheetRange) == false)
                    {
                        cmd.CommandText += $", range='{gridModel.SheetRange}'";
                    }
                }
                cmd.CommandText += ")";
                cmd.ExecuteNonQuery();
            }
        }
    }
}