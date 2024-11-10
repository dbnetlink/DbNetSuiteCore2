using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Html;
using System.Data;
using DbNetSuiteCore.Models;
using DocumentFormat.OpenXml.EMMA;

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
        public IEnumerable<DataRow> Rows => SelectModel.Data.AsEnumerable();
        public bool SelectFirstOption => SelectModel.RowSelection != RowSelection.None && string.IsNullOrEmpty(SelectModel.EmptyOption);
        public string HxTarget => $"next div.target";
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
