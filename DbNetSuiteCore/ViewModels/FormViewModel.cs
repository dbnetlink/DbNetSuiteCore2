﻿using System.Data;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Enums;

namespace DbNetSuiteCore.ViewModels
{
    public class FormViewModel : ComponentViewModel
    {
        public IEnumerable<FormColumn> Columns => _formModel.Columns;
        private readonly FormModel _formModel = new FormModel();
        public FormModel FormModel => _formModel;
        public int RecordCount => FormModel.PrimaryKeyValues.Count;
        public int CurrentRecord => FormModel.CurrentRecord;
        public string SelectId => _formModel.Id;
        public string LinkedFormIds => string.Join(",", FormModel.LinkedFormIds);
        public string HxTarget => $"{(FormModel.ToolbarPosition == ToolbarPosition.Bottom ? "previous" : "next")} div.form-body";
        public DataRow Record => FormModel.Data.Rows[0];
        public string SearchInput => FormModel.SearchInput;
        public FormViewModel(FormModel formModel) : base(formModel)
        {
            _formModel = formModel;
        }

        public SelectColumn? GetColumnInfo(DataColumn column)
        {
            return _GetColumnInfo(column, _formModel.Columns.Cast<ColumnModel>()) as SelectColumn;
        }
    }
}