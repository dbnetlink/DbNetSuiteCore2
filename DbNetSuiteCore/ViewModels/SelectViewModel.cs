using DbNetSuiteCore.Enums;
using System.Data;
using DbNetSuiteCore.Models;

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
        public bool SelectFirstOption => SelectModel.RowSelection != RowSelection.None && string.IsNullOrEmpty(SelectModel.EmptyOption);
        public string HxTarget => $"next div.target";
        public string Value(DataRow dataRow) 
        {
            return RowValue(dataRow, GetDataColumn(SelectModel.ValueColumn));
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
    }
}
