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
        public IEnumerable<DataRow> Rows => GridModel.Data.AsEnumerable().Skip((GridModel.CurrentPage - 1) * GridModel.PageSize).Take(GridModel.PageSize);
        public int TotalPages => (int)Math.Ceiling((double)GridModel.Data.Rows.Count / GridModel.PageSize);
        public int RowCount => GridModel.Data.Rows.Count;
        public string GridId => _gridModel.Id;
        public string TBodyId => $"tbody{_gridModel.Id}";
        public string LinkedGridId => _gridModel.LinkedGrid?.Id ?? string.Empty;
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
                _gridModel.PageSize = gridModel.Data.Rows.Count;
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

        public string? PrimaryKeyValue(DataRow dataRow)
        {
            if (DataSourceType == DataSourceType.FileSystem)
            {
                return Convert.ToString(RowValue(dataRow, "Name", false));
            }
            else
            {
                var primaryKeyColumn = GridModel.Columns.FirstOrDefault(c => c.PrimaryKey);
                if (primaryKeyColumn != null)
                {
                    var dataColumn = dataRow.Table.Columns.Cast<DataColumn>().ToList().FirstOrDefault(c => c.ColumnName == primaryKeyColumn.Name || primaryKeyColumn.Name.Split(".").Last() == c.ColumnName);

                    if (dataColumn != null)
                    {
                        return dataRow[dataColumn].ToString();
                    }
                }

                return null;
            }
        }

        public bool IsFolder(DataRow dataRow)
        {
            return Convert.ToBoolean(RowValue(dataRow, "IsDirectory", false));
        }

        public int SelectWidth(List<KeyValuePair<string, string>> options)
        {
            return options.Select(o => o.Value.Length).OrderByDescending(l => l).First();
        }

        private object RowValue(DataRow dataRow, string columnName, object defaultValue)
        {
            var dataColumn = dataRow.Table.Columns.Cast<DataColumn>().ToList().FirstOrDefault(c => c.ColumnName == columnName);

            if (dataColumn != null)
            {
                return dataRow[dataColumn];
            }

            return defaultValue;
        }

        private string PageUrl(int pageNumber)
        {
            return $"/gridcontrol.htmx?page={pageNumber}";
        }

    }
}
