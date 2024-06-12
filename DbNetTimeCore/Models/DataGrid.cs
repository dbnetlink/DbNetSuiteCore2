using System.Data;
namespace DbNetTimeCore.Models
{
    public class DataGrid
    {
        public IEnumerable<DataRow> Rows { get; set; } = new List<DataRow>();
        public IEnumerable<DataColumn> Columns { get; set; } = new List<DataColumn>();
        public string GridId => $"{Id}Grid";
        public int TotalPages { get; set; } = 0;
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string Id { get; set; } = string.Empty;
        public string SearchInput { get; set; } = string.Empty;
        public string NextPageUrl => NextPage ? $"/{Id}/?page={CurrentPage + 1}" : string.Empty;
        public string PreviousPageUrl => PreviousPage ? $"/{Id}/?page={CurrentPage - 1}" : string.Empty;
        public string SearchUrl => $"/{Id}/?handler=search";
        public string EditUrl => $"/{Id}/?handler=edit";
        public bool NextPage => CurrentPage < TotalPages;
        public bool PreviousPage => CurrentPage > 1;
        public List<ColumnInfo> ColumnInfo { get; set; } = new List<ColumnInfo>();
        public DataGrid() { }

        public ColumnInfo? GetColumnInfo(DataColumn column)
        {
            return ColumnInfo.FirstOrDefault(c => c.Name.Split(".").Last() == column.ColumnName);
        }

        public DataGrid(DataTable dataTable, string id, GridParameters gridParameters)
        {
            Id = id;
            CurrentPage = gridParameters.CurrentPage;
            SearchInput = gridParameters.SearchInput;
            ColumnInfo = gridParameters.Columns;
            Rows = dataTable.AsEnumerable().Skip((CurrentPage-1) * PageSize).Take(PageSize);
            Columns = dataTable.Columns.Cast<DataColumn>();
            TotalPages = (int)Math.Ceiling((double)dataTable.Rows.Count / PageSize);

            foreach(DataColumn column in Columns)
            {
                ColumnInfo? columnInfo = GetColumnInfo(column);

                if (columnInfo != null)
                {
                    if (columnInfo.DataType == typeof(string))
                    {
                        columnInfo.DataType = column.DataType;
                    }
                }
            }
        }
    }
}
