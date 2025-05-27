using DbNetSuiteCore.Models;
using DbNetSuiteCore.Enums;
using System.Data;
using DbNetSuiteCore.Extensions;
using Microsoft.Extensions.FileProviders;
using DbNetSuiteCore.Helpers;
using System.Text.RegularExpressions;

namespace DbNetSuiteCore.Repositories
{
    public class FileSystemRepository : IFileSystemRepository
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private static readonly HttpClient _httpClient = new HttpClient();
        public FileSystemRepository(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }
        public async Task GetRecords(ComponentModel componentModel, HttpContext httpContext)
        {
            var dataTable = await BuildDataTable(componentModel, httpContext);
            componentModel.Data = dataTable;

            string filterPart = string.Empty;
            string orderPart = string.Empty;
            if (componentModel is GridModel)
            {
                var gridModel = (GridModel)componentModel;
                filterPart = AddFilterPart(gridModel);
                orderPart = AddOrderPart(gridModel);
            }

            if (componentModel is SelectModel)
            {
                var selectModel = (SelectModel)componentModel;
                filterPart = AddFilterPart(selectModel);
                orderPart = AddOrderPart(selectModel);
            }

            var rows = dataTable.Select(filterPart, orderPart);

            if (rows.Any())
            {
                componentModel.Data = rows.CopyToDataTable();
            }
        }

        public async Task<DataTable> GetColumns(ComponentModel componentModel, HttpContext httpContext)
        {
            return await BuildDataTable(componentModel, httpContext);
        }

        public static void UpdateUrl(GridModel gridModel)
        {
            var folderSeparator = "/";

            if (TextHelper.IsAbsolutePath(gridModel.Url))
            {
                folderSeparator = "\\";
            }

            var urlParts = gridModel.Url.Split(folderSeparator);

            if (string.IsNullOrEmpty(gridModel.ParentKey) == false)
            {
                var json = TextHelper.DeobfuscateString(gridModel.ParentKey);
                var url = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(json) ?? string.Empty;
                urlParts = urlParts.Append(url).ToArray();

                gridModel.Url = string.Join(folderSeparator, urlParts.ToArray());
                gridModel.ParentKey = string.Empty;
            }
        }

        private async Task<DataTable> BuildDataTable(ComponentModel componentModel, HttpContext httpContext)
        {
            if (string.IsNullOrEmpty(componentModel.Url))
            {
                return GetEmptyDataTable();
            }
            var path = string.Empty;

            if (TextHelper.IsAbsolutePath(componentModel.Url))
            {
                path = componentModel.Url;
            }
            else
            {
                var pathParts = _env.WebRootPath.Split("\\");
                var urlParts = componentModel.Url.Split("/");

                foreach (var part in urlParts)
                {
                    if (part == "..")
                    {
                        pathParts = pathParts.Take(pathParts.Count() - 1).ToArray();
                    }
                    else
                    {
                        pathParts = pathParts.Append(part).ToArray();
                    }
                }
                path = string.Join("\\", pathParts);
            }

            return Tabulate(Contents(path), componentModel);
        }

        private IDirectoryContents Contents(string path)
        {
            var provider = new PhysicalFileProvider(path);
            return provider.GetDirectoryContents(string.Empty);
        }

        private DataTable GetEmptyDataTable()
        {
            DataTable dataTable = new DataTable();
            dataTable.Clear();
            dataTable.Columns.Add(FileSystemColumn.Icon.ToString(), typeof(bool));
            dataTable.Columns.Add(FileSystemColumn.IsDirectory.ToString(), typeof(bool));
            dataTable.Columns.Add(FileSystemColumn.Name.ToString(), typeof(string));
            dataTable.Columns.Add(FileSystemColumn.Extension.ToString(), typeof(string));
            dataTable.Columns.Add(FileSystemColumn.Length.ToString(), typeof(Int64));
            dataTable.Columns.Add(FileSystemColumn.Folder.ToString(), typeof(string));
            dataTable.Columns.Add(FileSystemColumn.Path.ToString(), typeof(string));
            dataTable.Columns.Add(FileSystemColumn.LastModified.ToString(), typeof(DateTime));
            return dataTable;
        }

        private DataTable Tabulate(IDirectoryContents directoryContents, ComponentModel componentModel)
        {
            var dataTable = GetEmptyDataTable();

            var contentColumns = (componentModel is GridModel) ? (componentModel as GridModel)!.ContentColumns : new List<GridColumn>();

            var i = 0;
            foreach (GridColumn gridColumn in contentColumns)
            {
                var name = $"{FileSystemColumn.Content}{i}";
                gridColumn.Expression = name;
                dataTable.Columns.Add(name, typeof(string));
            }

            foreach (IFileInfo file in directoryContents)
            {
                if (componentModel is SelectModel && file.IsDirectory)
                {
                    var selectModel = (SelectModel)componentModel;

                    if (selectModel.IsGrouped && selectModel.OptionGroupColumn.ColumnName == FileSystemColumn.Folder.ToString())
                    {
                        AddSubFolder(file, dataTable);
                        continue;
                    }
                }

                AddRow(file, dataTable, contentColumns);
            };

            return dataTable;
        }

        private void AddRow(IFileInfo file, DataTable dataTable, IEnumerable<GridColumn>? contentColumns = null)
        {
            DataRow dataRow = dataTable.NewRow();
            dataRow[FileSystemColumn.Icon.ToString()] = file.IsDirectory;
            dataRow[FileSystemColumn.IsDirectory.ToString()] = file.IsDirectory;
            dataRow[FileSystemColumn.Name.ToString()] = file.Name;
            dataRow[FileSystemColumn.Extension.ToString()] = file.IsDirectory ? string.Empty : file.Name.Split(".").Last();
            dataRow[FileSystemColumn.Length.ToString()] = file.IsDirectory ? System.DBNull.Value : file.Length;
            dataRow[FileSystemColumn.LastModified.ToString()] = file.LastModified.UtcDateTime;

            var path = GetPath(file.PhysicalPath);
            dataRow[FileSystemColumn.Folder.ToString()] = file.IsDirectory ? file.Name : (path.Split("/").Count() > 1 ? path.Split("/").Reverse().Skip(1).First() : string.Empty);
            dataRow[FileSystemColumn.Path.ToString()] = path;

            if (file.IsDirectory == false && (contentColumns ?? new List<GridColumn>()).Any() && file.Length < (1024 * 16))
            {
                var content = ReadFileContent(file);

                foreach (GridColumn gridColumn in contentColumns ?? new List<GridColumn>())
                {
                    var match = Regex.Match(content, gridColumn.RegularExpression, RegexOptions.IgnoreCase);
                    dataRow[gridColumn.Expression] = match.Success ? match.Groups[1].Value : string.Empty;
                }
            }

            dataTable.Rows.Add(dataRow);
        }

        private void AddSubFolder(IFileInfo folder, DataTable dataTable)
        {
            var contents = Contents(folder.PhysicalPath);

            foreach (IFileInfo file in contents)
            {
                AddRow(file, dataTable);
            };
        }

        private string GetPath(string? physicalPath)
        {
            if (physicalPath == null)
            {
                return string.Empty;
            }

            return physicalPath.Replace(_env.WebRootPath, string.Empty).Replace("\\", "/");
        }

        private static string ReadFileContent(IFileInfo fileInfo)
        {
            using (var reader = new StreamReader(fileInfo.CreateReadStream()))
            {
                return reader.ReadToEnd();
            }
        }

        private string AddFilterPart(GridModel gridModel)
        {
            string filter = string.Empty;
            List<string> filterParts = new List<string>();
            if (string.IsNullOrEmpty(gridModel.SearchInput) == false)
            {
                List<string> searchFilterPart = new List<string>();

                foreach (var col in gridModel.SearchableColumns.Select(c => c.Name).ToList())
                {
                    searchFilterPart.Add($"{col} like '%{gridModel.SearchInput}%'");
                }

                if (searchFilterPart.Any())
                {
                    filterParts.Add($"({string.Join(" or ", searchFilterPart)})");
                }
            }

            if (gridModel.FilterColumns.Any())
            {
                List<string> columnFilterPart = new List<string>();
                for (var i = 0; i < gridModel.ColumnFilter.Count; i++)
                {
                    if (string.IsNullOrEmpty(gridModel.ColumnFilter[i]))
                    {
                        continue;
                    }

                    var column = gridModel.Columns.Skip(i).First();

                    var columnFilter = GridModelExtensions.ParseFilterColumnValue(gridModel.ColumnFilter[i], column);

                    if (columnFilter != null)
                    {
                        columnFilterPart.Add($"{column.Name} {columnFilter.Value.Key} {Quoted(column)}{columnFilter.Value.Value}{Quoted(column)}");
                    }
                }

                if (columnFilterPart.Any())
                {
                    filterParts.Add($"({string.Join(" and ", columnFilterPart)})");
                }
            }

            string searchDialogFilter = Extensions.DataTableExtensions.AddSearchDialogFilterPart(gridModel);
            if (string.IsNullOrEmpty(searchDialogFilter) == false)
            {
                filterParts.Add(searchDialogFilter);
            }

            if (!string.IsNullOrEmpty(gridModel.FixedFilter))
            {
                filterParts.Add($"({gridModel.FixedFilter})");
            }

            return String.Join(" and ", filterParts);
        }


        private string AddFilterPart(SelectModel selectModel)
        {
            string filter = string.Empty;
            List<string> filterParts = new List<string>();
            if (string.IsNullOrEmpty(selectModel.SearchInput) == false)
            {
                List<string> searchFilterPart = new List<string>();

                foreach (var col in selectModel.SearchableColumns.Select(c => c.Name).ToList())
                {
                    searchFilterPart.Add($"{col} like '%{selectModel.SearchInput}%'");
                }

                if (searchFilterPart.Any())
                {
                    filterParts.Add($"({string.Join(" or ", searchFilterPart)})");
                }
            }

            if (!string.IsNullOrEmpty(selectModel.FixedFilter))
            {
                filterParts.Add($"({selectModel.FixedFilter})");
            }

            return String.Join(" and ", filterParts);
        }

        private string Quoted(GridColumn column)
        {
            return (new string[] { nameof(String), nameof(DateTime) }).Contains(column.DataTypeName) ? "'" : string.Empty;
        }

        private string AddOrderPart(GridModel gridModel)
        {
            if (string.IsNullOrEmpty(gridModel.SortColumnName))
            {
                return string.Empty;
            }

            return $"IsDirectory desc, {gridModel.SortColumnName} {gridModel.SortSequence}";
        }

        private string AddOrderPart(SelectModel selectModel)
        {
            string optionGroupSortColumnName = string.Empty;

            if (selectModel.IsGrouped)
            {
                optionGroupSortColumnName = $"{TextHelper.DelimitColumn(selectModel.OptionGroupColumn.ColumnName, selectModel.DataSourceType)},";
                return $"{optionGroupSortColumnName} {selectModel.SortColumnName} {selectModel.SortSequence}";
            }

            return $"IsDirectory desc,{selectModel.SortColumnName} {selectModel.SortSequence}";
        }
    }
}
