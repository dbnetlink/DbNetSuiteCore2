using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Html;
using System.Data;

namespace DbNetSuiteCore.Models
{
    public class GridViewModel : ComponentViewModel
    {
        private readonly GridModel _gridModel = new GridModel();
        public GridModel GridModel => _gridModel;
        public IEnumerable<DataRow> Rows { get; set; } = new List<DataRow>();
        public int TotalPages { get; set; } = 0;
        public string GridId => _gridModel.Id;
        public string TBodyId => $"tbody{_gridModel.Id}";
        public string LinkedGridId => _gridModel.LinkedGrid?.Id ?? string.Empty;
        public int CurrentPage => _gridModel.CurrentPage;
        public int PageSize => _gridModel.PageSize;
        public string TableName => _gridModel.TableName;
        public string ConnectionAlias => _gridModel.ConnectionAlias;
        public string SearchInput => _gridModel.SearchInput;
        public string SortColumn => _gridModel.SortColumn;
        public string SortKey => _gridModel.SortKey;
        public string CurrentSortKey => string.IsNullOrEmpty(SortKey) ? _gridModel.CurrentSortKey : SortKey;
        public bool CurrentSortAscending => _gridModel.SortSequence == SortOrder.Asc;
        public HtmlString SortIcon => CurrentSortAscending ? IconHelper.ArrowUp() : IconHelper.ArrowDown();
        public DataSourceType DataSourceType => _gridModel.DataSourceType;
        public RenderMode RenderMode { get; set; } = RenderMode.Page;

        public string HxTarget => $"{(GridModel.ToolbarPosition == ToolbarPosition.Bottom ? "previous" : "next")} tbody";

        public bool IsSortColumn(ColumnModel columnInfo)
        {
            return columnInfo.Key == CurrentSortKey;
        }
        public GridViewModel(GridModel gridModel) : base(gridModel)
        {
            _gridModel = gridModel;
            TotalPages = (int)Math.Ceiling((double)gridModel.Data.Rows.Count / PageSize);

            if (_gridModel.CurrentPage > TotalPages)
            {
                _gridModel.CurrentPage = TotalPages;
            }

            if (_gridModel.ToolbarPosition == ToolbarPosition.Hidden)
            {
                _gridModel.PageSize = gridModel.Data.Rows.Count;
            }

            Rows = gridModel.Data.AsEnumerable().Skip((CurrentPage - 1) * PageSize).Take(PageSize);
            Columns = gridModel.Data.Columns.Cast<DataColumn>();

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

        public DataColumn? GetDataColumn(GridColumnModel column)
        {
            return Columns.FirstOrDefault(c => c.ColumnName == column.Name || c.ColumnName == column.ColumnName);
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
