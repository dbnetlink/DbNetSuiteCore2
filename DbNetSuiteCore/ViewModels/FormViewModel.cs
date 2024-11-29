using System.Data;
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
        public string HxTarget => "closest div.form-and-toolbar";
        public DataRow Record => FormModel.Data.Rows.Count == 0 ? FormModel.Data.NewRow() : FormModel.Data.Rows[0];
        public object RecordId => FormModel.RecordId;
        public string SearchInput => FormModel.SearchInput;
        public bool HideToolbar => FormModel.IsLinked && string.IsNullOrEmpty(FormModel.ParentKey);
        public FormMode Mode => FormModel.Mode;
        public bool ReadOnly => FormModel.ReadOnly;
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
