using System.Data;
namespace DbNetTimeCore.Models
{
    public class DataGrid
    {
        public IEnumerable<DataRow> Rows { get; set; } = new List<DataRow>();
        public IEnumerable<DataColumn> Columns { get; set; } = new List<DataColumn>();
        public string BodyId => $"{BaseUrl}GridRows";
        public int TotalPages { get; set; } = 0;
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string BaseUrl { get; set; } = string.Empty;
        public string NextPageUrl => NextPage ? $"/{BaseUrl}/?page={CurrentPage + 1}" : string.Empty;
        public string PreviousPageUrl => PreviousPage ? $"/{BaseUrl}/?page={CurrentPage - 1}" : string.Empty;
        public bool NextPage => CurrentPage < TotalPages;
        public bool PreviousPage => CurrentPage > 1;
        public DataGrid() { }

        public DataGrid(DataTable dataTable, string baseUrl, int currentPage = 1)
        {
            BaseUrl = baseUrl;
            CurrentPage = currentPage;
            Rows = dataTable.AsEnumerable().Skip((CurrentPage-1) * PageSize).Take(PageSize);
            Columns = dataTable.Columns.Cast<DataColumn>();
            TotalPages = (int)Math.Ceiling((double)dataTable.Rows.Count / PageSize);
        }
    }
}
