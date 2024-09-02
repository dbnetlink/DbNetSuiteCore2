using DbNetSuiteCore.Models;
using Newtonsoft.Json;
using System.Data;
using DbNetSuiteCore.Extensions;
using Newtonsoft.Json.Linq;

namespace DbNetSuiteCore.Repositories
{
    public class JSONRepository : IJSONRepository
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private static readonly HttpClient _httpClient = new HttpClient();
        public JSONRepository(IConfiguration configuration, IWebHostEnvironment env)
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
                gridModel.ConvertEnumLookups();
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

        private async Task<DataTable> BuildDataTable(GridModel gridModel, HttpContext httpContext)
        {
            var url = gridModel.Url;

            if (url.StartsWith("http") == false)
            {
                url = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}";
            }
            string json = await _httpClient.GetStringAsync(url);

            DataTable? dataTable = new();
            if (string.IsNullOrWhiteSpace(json))
            {
                return dataTable;
            }
            dataTable = Tabulate(json);

            if (gridModel.Columns.Any())
            {
                string[] selectedColumns = gridModel.Columns.Select(c => c.Expression).ToArray();

                dataTable = new DataView(dataTable).ToTable(false, selectedColumns);
            }

            return dataTable;
        }

        private DataTable Tabulate(string json)
        {
            JArray srcArray = JArray.Parse(json);
            JArray trgArray = new JArray();
            foreach (JObject row in srcArray.Children<JObject>())
            {
                var cleanRow = new JObject();
                foreach (JProperty column in row.Properties())
                {
                    if (column.Value is JValue)
                    {
                        cleanRow.Add(column.Name, column.Value);
                    }
                }

                trgArray.Add(cleanRow);
            }

            return JsonConvert.DeserializeObject<DataTable>(trgArray.ToString());
        }

        private string AddFilterPart(GridModel gridModel)
        {
            string filter = string.Empty;
            List<string> filterParts = new List<string>();
            if (string.IsNullOrEmpty(gridModel.SearchInput) == false)
            {
                List<string> searchFilterPart = new List<string>();

                foreach (var col in gridModel.GridColumns.Where(c => c.Searchable).Select(c => c.Name).ToList())
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

            if (gridModel.IsNested || gridModel.IsLinked)
            {
                if (!string.IsNullOrEmpty(gridModel.ParentKey))
                {
                    var foreignKeyColumn = gridModel.Columns.FirstOrDefault(c => c.ForeignKey);
                    if (foreignKeyColumn != null)
                    {
                        filterParts.Add($"({foreignKeyColumn.Name} = {Quoted(foreignKeyColumn)}{gridModel.ParentKey}{Quoted(foreignKeyColumn)})");
                    }
                }
                else
                {
                    filterParts.Add("(1=2)");
                }
            }

            if (!string.IsNullOrEmpty(gridModel.FixedFilter))
            {
                filterParts.Add($"({gridModel.FixedFilter})");
            }

            return String.Join(" and ", filterParts);
        }

        private string Quoted(GridColumnModel column)
        {
            return (new string[] { nameof(String), nameof(DateTime) }).Contains(column.DataTypeName) ? "'" : string.Empty;
        }

        private string AddOrderPart(GridModel gridModel)
        {
            if (string.IsNullOrEmpty(gridModel.SortColumnName))
            {
                return string.Empty;
            }

            return $"{gridModel.SortColumnName} {gridModel.SortSequence}";
        }
    }
}
