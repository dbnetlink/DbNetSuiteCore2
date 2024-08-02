using DbNetTimeCore.Enums;
using System.Data;
namespace DbNetTimeCore.Models
{
    public class GridViewModel : ComponentViewModel
    {
        private readonly GridModel _gridModel = new GridModel();

        public GridModel GridModel => _gridModel;
        public IEnumerable<DataRow> Rows { get; set; } = new List<DataRow>();
        public int TotalPages { get; set; } = 0;
        public string GridId => _gridModel.Id;
        public string IndicatorId => _gridModel.IndicatorId;
        public string ContainerId => _gridModel.ContainerId;
        public int CurrentPage => _gridModel.CurrentPage;
        public int PageSize => _gridModel.PageSize;
        public string TableName => _gridModel.TableName;
        public string ConnectionAlias => _gridModel.ConnectionAlias;
        public string SearchInput => _gridModel.SearchInput;
        public string SortColumn => _gridModel.SortColumn;
        public string SortKey => _gridModel.SortKey;
        public string CurrentSortKey => string.IsNullOrEmpty(SortKey) ? _gridModel.CurrentSortKey : SortKey;
        public bool CurrentSortAscending => (_gridModel.SortSequence ?? "asc") == "asc";
        public string FirstPageUrl => PreviousPage ? PageUrl(1) : string.Empty;
        public string LastPageUrl => NextPage ? PageUrl(TotalPages) : string.Empty;
        public string NextPageUrl => NextPage ? PageUrl(CurrentPage + 1) : string.Empty;
        public string PreviousPageUrl => PreviousPage ? PageUrl(CurrentPage - 1) : string.Empty;
        public DataSourceType DataSourceType => _gridModel.DataSourceType;

        public bool IsSortColumn(ColumnModel columnInfo)
        {
            return columnInfo.Key == CurrentSortKey;
        }
        public bool NextPage => CurrentPage < TotalPages;
        public bool PreviousPage => CurrentPage > 1;
        public GridViewModel(DataTable dataTable, GridModel gridModel) : base(dataTable, gridModel)
        {
            _gridModel = gridModel;
            TotalPages = (int)Math.Ceiling((double)dataTable.Rows.Count / PageSize);

            if (_gridModel.CurrentPage > TotalPages)
            {
                _gridModel.CurrentPage = TotalPages;
            }

            Rows = dataTable.AsEnumerable().Skip((CurrentPage - 1) * PageSize).Take(PageSize);
            Columns = dataTable.Columns.Cast<DataColumn>();

            foreach (DataColumn column in Columns)
            {
                ColumnModel? columnInfo = GetColumnInfo(column);

                if (columnInfo != null)
                {
                    if (columnInfo.DataType == typeof(string))
                    {
                        columnInfo.DataType = column.DataType;
                    }
                }
            }
        }

        public GridColumnModel? GetColumnInfo(DataColumn column)
        {
            return (GridColumnModel)_GetColumnInfo(column);
        }

        private string PageUrl(int pageNumber)
        {
            return $"/gridcontrol.htmx?page={pageNumber}";
        }

    }
}
