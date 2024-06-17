using SQLitePCL;
using System.Data;
namespace DbNetTimeCore.Models
{
    public class DataGrid
    {
        private readonly GridParameters _gridParameters = new GridParameters();
        public IEnumerable<DataRow> Rows { get; set; } = new List<DataRow>();
        public IEnumerable<DataColumn> Columns { get; set; } = new List<DataColumn>();

        public string GridId => $"{Id}Grid";
        public int TotalPages { get; set; } = 0;
        public int CurrentPage => _gridParameters.CurrentPage;
        public int PageSize { get; set; } = 20;
        public string Id { get; set; } = string.Empty;
        public string SearchInput => _gridParameters.SearchInput;
        public string SortColumn => _gridParameters.SortColumn;
        public int ColSpan => _gridParameters.ColSpan;
        public string SortKey => _gridParameters.SortKey;
        public string CurrentSortKey => string.IsNullOrEmpty(SortKey) ? _gridParameters.CurrentSortKey : SortKey;
        public bool CurrentSortAscending => (_gridParameters.SortSequence ?? "asc") == "asc";
        public string FirstPageUrl => PreviousPage ? PageUrl(1) : string.Empty;
        public string LastPageUrl => NextPage ? PageUrl(TotalPages) : string.Empty;
        public string NextPageUrl => NextPage ? PageUrl(CurrentPage + 1) : string.Empty;
        public string PreviousPageUrl => PreviousPage ? PageUrl(CurrentPage - 1) : string.Empty;
        public string SearchUrl => $"/{Id}/?handler=search";
        public string Message => _gridParameters.Message;
        public string SaveUrl(DataRow row)
        {
            return $"/{Id}/?handler=save&pk={PrimaryKeyValue(row)}";
        }
        public string EditUrl(DataRow row)
        {
            return $"/{Id}/?handler=edit&pk={PrimaryKeyValue(row)}";
        }
        public bool NextPage => CurrentPage < TotalPages;
        public bool PreviousPage => CurrentPage > 1;
        public List<ColumnInfo> ColumnInfo => _gridParameters.Columns;
        public bool HasPrimaryKey => ColumnInfo.Any(c => c.IsPrimaryKey);
        public ColumnInfo? PrimaryKey => ColumnInfo.FirstOrDefault(c => c.IsPrimaryKey);
        public DataGrid() { }
        public DataGrid(DataTable dataTable, string id, GridParameters gridParameters)
        {
            _gridParameters = gridParameters;
            Id = id;
            TotalPages = (int)Math.Ceiling((double)dataTable.Rows.Count / PageSize);

            if (_gridParameters.CurrentPage > TotalPages)
            {
                _gridParameters.CurrentPage = TotalPages;
            }

            Rows = dataTable.AsEnumerable().Skip((CurrentPage - 1) * PageSize).Take(PageSize);
            Columns = dataTable.Columns.Cast<DataColumn>();

            foreach (DataColumn column in Columns)
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

        private string PageUrl(int pageNumber)
        {
            return $"/{Id}/?page={pageNumber}";
        }
        public ColumnInfo? GetColumnInfo(DataColumn column)
        {
            return ColumnInfo.FirstOrDefault(c => c.Name.Split(".").Last() == column.ColumnName);
        }

        public bool IsSortColumn(ColumnInfo columnInfo)
        {
            return columnInfo.Key == CurrentSortKey;
        }

        public object? PrimaryKeyValue(DataRow dataRow)
        {
            if (!HasPrimaryKey)
            {
                return null;
            }

            ColumnInfo column = PrimaryKey! as ColumnInfo;

            DataColumn? dataColumn = Columns.FirstOrDefault(c => c.ColumnName == column.Name.Split(".").Last());

            if (dataColumn == null)
            {
                return null;
            }

            return dataRow[dataColumn];
        }
    }
}
