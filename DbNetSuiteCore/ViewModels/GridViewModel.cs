using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Html;
using System.Data;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Constants;

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
        public string LinkedGridIds => string.Join(",", _gridModel.LinkedGridIds);
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


        public int SelectWidth(List<KeyValuePair<string, string>> options)
        {
            return options.Select(o => o.Value.Length).OrderByDescending(l => l).First();
        }


        public HtmlString RenderNestedButtons(DataRow row, HtmlString openIcon, HtmlString closedIcon)
        {
            List<HtmlString> html = new List<HtmlString>();
            html.Add(new HtmlString($"<div class=\"nested-icons\">"));
            html.Add(new HtmlString($"<span>{openIcon}</span>"));
            html.Add(new HtmlString($"<span style=\"display:none\">{closedIcon}</span>"));
            html.Add(new HtmlString($"<span "));
            html.Add(RazorHelper.Attribute($"name", TriggerNames.NestedGrid));
            html.Add(RazorHelper.Attribute($"hx-post", SubmitUrl));
            html.Add(RazorHelper.Attribute($"hx-target", "closest tr"));
            html.Add(RazorHelper.Attribute($"hx-indicator", "next .htmx-indicator"));
            html.Add(RazorHelper.Attribute($"hx-swap", "afterend"));
            html.Add(RazorHelper.Attribute($"hx-vals", $"{{\"primaryKey\":\"{GridModel.PrimaryKeyValue(row)}\"}}"));
            html.Add(RazorHelper.Attribute($"hx-trigger", "click once"));
            html.Add(RazorHelper.Attribute($"style", "display:none"));
            return new HtmlString(string.Join(" ", html));
        }

        public HtmlString RenderColumnSelectFilterRefresh(List<KeyValuePair<string, string>> options, string key)
        {
            List<HtmlString> html = new List<HtmlString>();
            html.Add(new HtmlString($"<select data-key=\"{key}\">"));
            AddColumnFilterOptions(html, options);
            html.Add(new HtmlString($"</select>"));
            return new HtmlString(string.Join(" ", html));
        }

        public HtmlString RenderColumnFilterError(GridColumn gridColumn)
        {
            return new HtmlString($"<span data-key=\"{gridColumn.Key}\">{gridColumn.FilterError}</span>");
        }

        public HtmlString RenderColumnFilter(GridColumn gridColumn)
        {
            if ((gridColumn?.Filter ?? FilterType.None) != FilterType.None)
            {
                if (gridColumn.LookupOptions != null && gridColumn.LookupOptions.Any())
                {
                    return RenderColumnSelectFilter(gridColumn.LookupOptions, gridColumn.Key);
                }
                else
                {
                    switch (gridColumn.DataTypeName)
                    {
                        case nameof(Boolean):
                            return RenderColumnSelectFilter(GridColumn.BooleanFilterOptions, gridColumn.Key);
                        default:
                            return new HtmlString($"<input class=\"w-full\" type=\"search\" name=\"columnFilter\" value=\"{SearchInput}\" hx-post=\"{SubmitUrl}\" hx-trigger=\"input changed delay:1000ms, search\" hx-target=\"next tbody\" hx-indicator=\"next .htmx-indicator\" hx-swap=\"outerHTML\" data-key=\"{gridColumn.Key}\" autocomplete=\"off\" />");
                    }
                }
            }

            return new HtmlString(string.Empty);
        }

        public HtmlString RenderColumnSelectFilter(List<KeyValuePair<string, string>> options, string key)
        {
            List<HtmlString> html = new List<HtmlString>();
            html.Add(new HtmlString($"<select class=\"column-filter\" name=\"columnFilter\" hx-post=\"{SubmitUrl}\" hx-trigger=\"change\" hx-target=\"next tbody\" hx-indicator=\"next .htmx-indicator\" hx-swap=\"outerHTML\" data-key=\"{key}\">"));
            AddColumnFilterOptions(html, options);
            html.Add(new HtmlString($"</select>"));


            return new HtmlString(string.Join(" ", html));
        }

        private void AddColumnFilterOptions(List<HtmlString> html, List<KeyValuePair<string, string>> options)
        {
            html.Add(new HtmlString($"<option value=\"\"></option>"));

            foreach (var option in options)
            {
                html.Add(new HtmlString($"<option value=\"{option.Key}\">{option.Value}</option>"));
            }
        }

        public HtmlString RenderPageNumber(int pageNumber, int totalPages)
        {
            List<HtmlString> html = new List<HtmlString>();
            html.Add(new HtmlString($"<select name=\"{TriggerNames.Page}\" value=\"{pageNumber}\" hx-post=\"{SubmitUrl}\" hx-target=\"{HxTarget}\" hx-indicator=\"next .htmx-indicator\" hx-swap=\"outerHTML\" style=\"padding-right:2em\">"));

            for (var i = 1; i <= totalPages; i++)
            {
                var selected = (i == pageNumber) ? " selected" : string.Empty;
                html.Add(new HtmlString($"<option value=\"{i}\"{selected}>{i}</option>"));
            }

            html.Add(new HtmlString($"</select>"));
            return new HtmlString(string.Join(" ", html));
        }

        public HtmlString RenderNavButton(string name, HtmlString icon, string title)
        {
            return new HtmlString($"<button type=\"button\" button-type=\"{name}\" title=\"{title}\" hx-post=\"{SubmitUrl}\" name=\"{name}\" hx-target=\"{HxTarget}\" hx-indicator=\"next .htmx-indicator\" hx-swap=\"outerHTML\">{icon}</button>");
        }

        public HtmlString RenderButton(string name, HtmlString icon, string title)
        {
            return new HtmlString($"<button class=\"\" type=\"button\" button-type=\"{name}\" title=\"{title}\" hx-post=\"{SubmitUrl}\" name=\"{name}\" hx-target=\"{HxTarget}\" hx-indicator=\"next .htmx-indicator\" hx-swap=\"outerHTML\">{icon}</button>");
        }

        public HtmlString RenderTotalPages(int totalPages)
        {
            return new HtmlString($"<input class=\"text-center\" style=\"width:{(totalPages.ToString().Length + 1)}em\" readonly type=\"text\" data-type=\"total-pages\" value=\"{totalPages}\" />");
        }

        public HtmlString RenderRowCount(int rowCount)
        {
            return new HtmlString($"<input class=\"text-center\" style=\"width:{(rowCount.ToString().Length + 1)}em\" readonly type=\"text\" data-type=\"row-count\" value=\"{rowCount}\" />");
        }


        public HtmlString RenderExportOptions()
        {
            var options = new List<string>() { "CSV", "Excel", "HTML", "JSON" };

            List<HtmlString> html = new List<HtmlString>();
            html.Add(new HtmlString($"<select class=\"\" name=\"exportformat\" style=\"width:5rem\">"));

            foreach (var option in options)
            {
                html.Add(new HtmlString($"<option value=\"{option.ToLower()}\">{option}</option>"));
            }
            html.Add(new HtmlString($"</select>"));
            return new HtmlString(string.Join(" ", html));
        }

        private string PageUrl(int pageNumber)
        {
            return $"/gridcontrol.htmx?page={pageNumber}";
        }
    }
}
