using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Html;
using System.Data;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.ViewModels
{
    public class GridViewModel : ComponentViewModel
    {
        public IEnumerable<GridColumn> Columns => _gridModel.Columns;
        public IEnumerable<GridColumn> VisibleColumns => _gridModel.VisbleColumns;
        public IEnumerable<GridColumn> DataOnlyColumns => _gridModel.DataOnlyColumns;
        private readonly GridModel _gridModel = new GridModel();
        public GridModel GridModel => _gridModel;
        public ViewDialog ViewDialog => _gridModel.ViewDialog!;
        public IEnumerable<DataRow> Rows => GridModel.Data.AsEnumerable().Skip((GridModel.CurrentPage - 1) * GridModel.PageSize).Take(GridModel.PageSize);
        public int TotalPages => RowCount == 0 ? 0 : (int)Math.Ceiling((double)RowCount / GridModel.PageSize);
        public int RowCount => GridModel.Data.Rows.Count;
        public string GridId => _gridModel.Id;
        public string TBodyId => $"tbody{_gridModel.Id}";
        public string ViewDialogId => $"viewDialog{_gridModel.Id}";
        public string LinkedGridIds => string.Join(",",_gridModel.LinkedGridIds);
        public string SearchInput => _gridModel.SearchInput;
        public string CurrentSortKey => _gridModel.CurrentSortKey;
        public HtmlString SortIcon => _gridModel.CurrentSortAscending ? IconHelper.ArrowUp() : IconHelper.ArrowDown();
        public DataSourceType DataSourceType => _gridModel.DataSourceType;
        public RenderMode RenderMode { get; set; } = RenderMode.Page;

        public string HxTarget => $"{(GridModel.ToolbarPosition == ToolbarPosition.Bottom ? "previous" : "next")} tbody";

        public GridViewModel(GridModel gridModel) : base(gridModel)
        {
            _gridModel = gridModel;

            if (_gridModel.CurrentPage > TotalPages)
            {
                _gridModel.CurrentPage = TotalPages;
            }

            if (_gridModel.ToolbarPosition == ToolbarPosition.Hidden)
            {
                _gridModel.PageSize = RowCount > 0 ? RowCount : _gridModel.PageSize;
            }

            foreach (DataColumn column in DataColumns)
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

        public GridColumn? GetColumnInfo(DataColumn column)
        {
            return _GetColumnInfo(column, _gridModel.Columns.Cast<ColumnModel>()) as GridColumn;
        }

        public DataColumn? GetDataColumn(GridColumn column)
        {
            return _gridModel.GetDataColumn(column);
        }


        public bool IsFolder(DataRow dataRow)
        {
            return Convert.ToBoolean(GridModel.RowValue(dataRow, "IsDirectory", false));
        }

        public int SelectWidth(List<KeyValuePair<string, string>> options)
        {
            return options.Select(o => o.Value.Length).OrderByDescending(l => l).First();
        }


        private string PageUrl(int pageNumber)
        {
            return $"/gridcontrol.htmx?page={pageNumber}";
        }

    }
}
