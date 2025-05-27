using DbNetSuiteCore.Enums;
using System.Data;
using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Html;
using DbNetSuiteCore.Helpers;
using DocumentFormat.OpenXml.EMMA;
using System.Text.Json;

namespace DbNetSuiteCore.ViewModels
{
    public class SelectViewModel : ComponentViewModel
    {
        public IEnumerable<SelectColumn> Columns => _selectModel.Columns;
        private readonly SelectModel _selectModel = new SelectModel();
        public SelectModel SelectModel => _selectModel;
        public int RowCount => SelectModel.Data.Rows.Count;
        public string SelectId => _selectModel.Id;
        public string LinkedSelectIds => string.Join(",", SelectModel.LinkedSelectIds);
        public DataRowCollection Rows => SelectModel.Data.Rows;
        public bool SelectFirstOption => SelectModel.RowSelection == RowSelection.Single && string.IsNullOrEmpty(SelectModel.EmptyOption);
        public string HxTarget => $"next div.target";
        public string Value(DataRow dataRow) 
        {
            return TextHelper.ObfuscateString(JsonSerializer.Serialize(new List<object>() { RowValue(dataRow, GetDataColumn(SelectModel.ValueColumn)) }));
        }
        public string Description(DataRow dataRow)
        {
            return RowValue(dataRow, GetDataColumn(SelectModel.DescriptionColumn));
        }
        public string GroupValue(DataRow dataRow)
        {
            return RowValue(dataRow,GetDataColumn(Columns.First(c => c.OptionGroup)));
        }

        private string RowValue(DataRow dataRow, DataColumn? dataColumn)
        {
            return dataRow[dataColumn!]?.ToString() ?? string.Empty;
        }

        public bool ChangeInGroup(int rowNumber)
        {
            return SelectModel.IsGrouped && (GroupValue(Rows[rowNumber]) != GroupValue(Rows[rowNumber-1]));
        }

        public SelectViewModel(SelectModel selectModel) : base(selectModel)
        {
            _selectModel = selectModel;
        }

        public SelectColumn? GetColumnInfo(DataColumn column)
        {
            return _GetColumnInfo(column, _selectModel.Columns.Cast<ColumnModel>()) as SelectColumn;
        }

        public HtmlString RenderNoRecordsOption()
        {
            return new HtmlString($"<option disabled selected value=\"\">{ResourceHelper.GetResourceString(ResourceNames.NoRecordsFound)}</option>");
        }

        public HtmlString RenderOption(DataRow row, int rowNumber)
        {
            var selected = rowNumber == 0 && SelectFirstOption ? " selected" : null;
            return new HtmlString($"<option value=\"{Value(row)}\"{selected} {RazorHelper.DataAttributes(row, SelectModel)}>{Description(row)}</option>");
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
