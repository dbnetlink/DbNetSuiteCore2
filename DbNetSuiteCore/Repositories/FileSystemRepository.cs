using DbNetSuiteCore.Models;
using DbNetSuiteCore.Enums;
using System.Data;
using DbNetSuiteCore.Extensions;
using Microsoft.Extensions.FileProviders;

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
        public async Task GetRecords(GridModel gridModel, HttpContext httpContext)
        {
            var dataTable = await BuildDataTable(gridModel, httpContext);

            var rows = dataTable.Select(AddFilterPart(gridModel), AddOrderPart(gridModel));

            if (rows.Any()) 
            {
                gridModel.Data = rows.CopyToDataTable(); 
            }
            else
            {
                gridModel.Data = new DataTable();
            }
        }

        public async Task<DataTable> GetColumns(GridModel gridModel, HttpContext httpContext)
        {
            return await BuildDataTable(gridModel, httpContext);
        }

        public static void UpdateUrl(GridModel gridModel)
        {
            var urlParts = gridModel.Url.Split("/");

            if (string.IsNullOrEmpty(gridModel.ParentKey) == false)
            {
                urlParts = urlParts.Append(gridModel.ParentKey).ToArray();
                gridModel.Url = string.Join("/", urlParts.ToArray());
                gridModel.ParentKey = string.Empty;
            }
        }

        private async Task<DataTable> BuildDataTable(GridModel gridModel, HttpContext httpContext)
        {
            var pathParts = _env.WebRootPath.Split("\\");
            var urlParts = gridModel.Url.Split("/");

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

            var path = string.Join("\\", pathParts);

            var provider = new PhysicalFileProvider(path);
            var contents = provider.GetDirectoryContents(string.Empty);

            return Tabulate(contents);
        }

        private DataTable Tabulate(IDirectoryContents directoryContents)
        {
            DataTable dataTable = new DataTable();
            dataTable.Clear();
            dataTable.Columns.Add(FileSystemColumn.Icon.ToString(), typeof(bool));
            dataTable.Columns.Add(FileSystemColumn.IsDirectory.ToString(), typeof(bool));
            dataTable.Columns.Add(FileSystemColumn.Name.ToString(), typeof(string));
            dataTable.Columns.Add(FileSystemColumn.Extension.ToString(), typeof(string));
            dataTable.Columns.Add(FileSystemColumn.Length.ToString(), typeof(Int64));
            dataTable.Columns.Add(FileSystemColumn.LastModified.ToString(), typeof(DateTime));

            foreach (IFileInfo file in directoryContents)
            {
                DataRow dataRow = dataTable.NewRow();
                dataRow[FileSystemColumn.Icon.ToString()] = file.IsDirectory;
                dataRow[FileSystemColumn.IsDirectory.ToString()] = file.IsDirectory;
                dataRow[FileSystemColumn.Name.ToString()] = file.Name;
                dataRow[FileSystemColumn.Extension.ToString()] = file.IsDirectory ? string.Empty : file.Name.Split(".").Last();
                dataRow[FileSystemColumn.Length.ToString()] = file.IsDirectory ? System.DBNull.Value : file.Length;
                dataRow[FileSystemColumn.LastModified.ToString()] = file.LastModified.UtcDateTime;
                dataTable.Rows.Add(dataRow);
            };

            return dataTable;
        }

        private string AddFilterPart(GridModel gridModel)
        {
            string filter = string.Empty;
            List<string> filterParts = new List<string>();
            if (string.IsNullOrEmpty(gridModel.SearchInput) == false)
            {
                List<string> searchFilterPart = new List<string>();

                foreach (var col in gridModel.Columns.Where(c => c.Searchable).Select(c => c.Name).ToList())
                {
                    searchFilterPart.Add($"{col} like '%{gridModel.SearchInput}%'");
                }

                if (searchFilterPart.Any())
                {
                    filterParts.Add($"({string.Join(" or ", searchFilterPart)})");
                }
            }

            if (gridModel.Columns.Any(c => c.Filter))
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

            if (!string.IsNullOrEmpty(gridModel.FixedFilter))
            {
                filterParts.Add($"({gridModel.FixedFilter})");
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
    }
}
