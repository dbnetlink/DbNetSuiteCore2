using DbNetTimeCore.Models;
using Newtonsoft.Json;
using System.Data;

namespace DbNetTimeCore.Repositories
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
        public async Task<DataTable> GetRecords(GridModel gridModel, HttpContext httpContext)
        {
            var dataTable = await BuildDataTable(gridModel, httpContext);

            return dataTable.Select(AddFilterPart(gridModel), AddOrderPart(gridModel)).CopyToDataTable();
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
            dataTable = JsonConvert.DeserializeObject<DataTable>(json);

            if (gridModel.Columns.Any())
            {
                string[] selectedColumns = gridModel.Columns.Select(c => c.Expression).ToArray();

                dataTable = new DataView(dataTable).ToTable(false, selectedColumns);

            }
            return dataTable;
        }

        private string AddFilterPart(GridModel gridModel)
        {
            string filter = string.Empty;
            if (string.IsNullOrEmpty(gridModel.SearchInput) == false)
            {
                List<string> filterPart = new List<string>();

                foreach (var col in gridModel.GridColumns.Where(c => c.Searchable).Select(c => c.Name).ToList())
                {
                    filterPart.Add($"{col} like '%{gridModel.SearchInput}%'");
                }

                if (filterPart.Any())
                {
                    filter = $"{string.Join(" or ", filterPart)}";
                }
            }

            return filter;
        }

        private string AddOrderPart(GridModel gridModel)
        {
            var sortColumn = gridModel.Columns[Convert.ToInt32(gridModel.SortColumn) - 1].ColumnName;
            var currentSortColumn = gridModel.Columns[Convert.ToInt32(gridModel.CurrentSortColumn) - 1].ColumnName;

            if (sortColumn == String.Empty)
            {
                return string.Empty;
            }

            return $"{(!string.IsNullOrEmpty(gridModel.SortKey) ? sortColumn : currentSortColumn)} {gridModel.SortSequence}";
        }
    }
}
