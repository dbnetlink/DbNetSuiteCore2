using DbNetSuiteCore.Enums;
using System.Data;
using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Html;
using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.ViewModels
{
    public class TreeViewModel : ComponentViewModel
    {
        public IEnumerable<TreeColumnViewModel> Columns => _treeModel.Columns.Select(c => new TreeColumnViewModel(c));
        private readonly TreeModel _treeModel = new TreeModel();
        public TreeModel TreeModel => _treeModel;
        public int RowCount => TreeModel.Data.Rows.Count;
        public string LinkedSelectIds => string.Join(",", TreeModel.LinkedSelectIds);
        public DataRowCollection Rows => TreeModel.Data.Rows;
        public bool SelectFirstOption => TreeModel.RowSelection != RowSelection.Multiple && string.IsNullOrEmpty(TreeModel.EmptyOption);
        public string HxTarget => $"next div.target";
        public bool IsGrouped => TreeModel.IsGrouped;
        public string Value(DataRow dataRow) 
        {
            return RowValue(dataRow, GetDataColumn(TreeModel.ValueColumn));
        }
        public string Description(DataRow dataRow)
        {
            return RowValue(dataRow, GetDataColumn(TreeModel.DescriptionColumn));
        }
        public string GroupValue(DataRow dataRow)
        {
            return RowValue(dataRow,GetDataColumn(Columns.Where(c => c.Column.OptionGroup).Select(c => c.Column).First()));
        }

        private string RowValue(DataRow dataRow, DataColumn dataColumn)
        {
            return dataRow[dataColumn!]?.ToString() ?? string.Empty;
        }

        public bool ChangeInGroup(int rowNumber)
        {
            return TreeModel.IsGrouped && (GroupValue(Rows[rowNumber]) != GroupValue(Rows[rowNumber-1]));
        }

        public TreeViewModel(TreeModel treeModel) : base(treeModel)
        {
            _treeModel = treeModel;
        }

        public SelectColumn GetColumnInfo(DataColumn column)
        {
            return _GetColumnInfo(column, _treeModel.Columns.Cast<ColumnModel>()) as SelectColumn;
        }

        public HtmlString RenderNoRecordsOption()
        {
            return new HtmlString($"<option disabled selected value=\"\">{ResourceHelper.GetResourceString(ResourceNames.NoRecordsFound)}</option>");
        }

        public HtmlString RenderLeaf(DataRow row, int rowNumber)
        {
            return new HtmlString($" <label class=\"leaf\"><input type=\"radio\" name=\"location\" value=\"Houston\"> Houston</label>");
        }

        public HtmlString OpenOptionGroup(DataRow row)
        {
            return new HtmlString($"<optgroup label=\"{GroupValue(row)}\">");
        }
        public HtmlString CloseOptionGroup()
        {
            return new HtmlString("</optgroup>");
        }
    }
}
