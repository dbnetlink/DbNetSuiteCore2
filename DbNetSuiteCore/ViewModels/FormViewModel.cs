using System.Data;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Extensions;
using Microsoft.AspNetCore.Html;
using DbNetSuiteCore.Constants;
using DbNetSuiteCore.Helpers;

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
        public DataRow Record => FormModel.Data.Rows.Count == 0 ? FormModel.Data.NewRow() : FormModel.Data.Rows[0];
        public object RecordId => FormModel.RecordId;
        public string HxTarget => "closest div.form-and-toolbar";
        public string SearchInput => FormModel.SearchInput;
        public bool HideToolbar => FormModel.IsLinked && FormModel.ParentModel.RowCount == 0 && FormModel.OneToOne == false;
        public FormMode Mode => FormModel.Mode;
        public bool ReadOnly => FormModel.ReadOnly;
        public bool RenderInsert => FormModel.Insert && Mode != FormMode.Insert && ReadOnly == false;
        public bool RenderDelete => FormModel.Delete && Mode == FormMode.Update && ReadOnly == false;
        public bool ShowNavigation => Mode == FormMode.Update && FormModel.OneToOne == false;
        public bool ShowQuickSearch => Mode != FormMode.Insert && FormModel.OneToOne == false && FormModel.SearchableColumns.Any();
        public bool ShowSearchDialog => Mode != FormMode.Insert && FormModel.OneToOne == false && SearchDialog;
        public bool RenderInsertDelete => RenderInsert || RenderDelete;
        public bool JustifyEnd => Mode == FormMode.Insert || (ShowNavigation == false && ShowQuickSearch == false);
        public string ConfirmDialogId => $"confirmDialog{_formModel.Id}";
        public FormMode? CommitType => FormModel.CommitType;


        public FormViewModel(FormModel formModel) : base(formModel)
        {
            _formModel = formModel;
        }

        public SelectColumn? GetColumnInfo(DataColumn column)
        {
            return _GetColumnInfo(column, _formModel.Columns.Cast<ColumnModel>()) as SelectColumn;
        }

        public KeyValuePair<string, string> GetColumnValues(FormColumn formColumn)
        {
            object? value = null;
            object? dbValue = null;

            switch (FormModel.Mode)
            {
                case FormMode.Update:
                    DataColumn? dataColumn = GetDataColumn(formColumn);
                    dbValue = (dataColumn == null) ? string.Empty : formColumn.FormatValue(Record[dataColumn]);
                    value = dbValue;
                    break;
                case FormMode.Insert:
                    if (formColumn.PrimaryKey == false)
                    {
                        value = formColumn.GetInitialValue();
                    }
                    dbValue = "";
                    value = dbValue;
                    break;
            }

            if (FormModel.FormValues.Keys.Contains(formColumn.ColumnName))
            {
                value = FormModel.FormValues[formColumn.ColumnName];
            }

            return new KeyValuePair<string, string>(formColumn.ToStringOrEmpty(dbValue), formColumn.ToStringOrEmpty(value));
        }

        public HtmlString RenderRecordNumber(int recordNumber, int recordCount)
        {
            List<HtmlString> html = new List<HtmlString>();
            html.Add(new HtmlString($"<select name=\"{TriggerNames.Record}\" value=\"{recordNumber}\" hx-post=\"{SubmitUrl}\" hx-target=\"{HxTarget}\" hx-indicator=\"next .htmx-indicator\" hx-swap=\"outerHTML\" style=\"padding-right:2em\">"));

            for (var i = 1; i <= recordCount; i++)
            {
                var selected = i == recordNumber ? " selected" : string.Empty;
                html.Add(new HtmlString($"<option value=\"{i}\"{selected}>{i}</option>"));
            }
            html.Add(new HtmlString($"</select>"));
            return new HtmlString(string.Join(" ", html));
        }

        public HtmlString RenderButton(string name, HtmlString icon, ResourceNames resourceName, bool disabled = false, string style = "")
        {
            Dictionary<string, string> attributes = new Dictionary<string, string>();

            if (name == TriggerNames.Delete)
            {
                attributes["hx-confirm-dialog"] = ResourceHelper.GetResourceString(ResourceNames.ConfirmDelete);
            }
            var disabledAttr = disabled ? " disabled" : null;
            return new HtmlString($"<button type=\"button\" style=\"{style}\" button-type=\"{name}\" title=\"{this.ButtonText(resourceName)}\" hx-post=\"{SubmitUrl}\" name=\"{name}\" hx-trigger=\"click\" hx-target=\"{HxTarget}\" hx-indicator=\"next .htmx-indicator\" hx-swap=\"outerHTML\" {disabledAttr} {RazorHelper.Attributes(attributes)}>{icon}</button>");
        }

        public string Justify()
        {
            if (FormModel.OneToOne)
            {
                return Mode == FormMode.Insert ? "justify-end" : (RenderInsertDelete ? "justify-between" : "justify-end");
            }
            else
            {
                return JustifyEnd ? "justify-end" : "justify-between";
            }
        }

        public HtmlString RenderRecordCount(int recordCount)
        {
            return new HtmlString($"<input class=\"text-center\" style=\"width:{(recordCount.ToString().Length + 1)}em\" readonly type=\"text\" data-type=\"record-count\" value=\"{recordCount}\" />");
        }

    }
}
