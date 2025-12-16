using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.ViewModels
{
    public class GridColumnViewModel : ColumnViewModel
    {
        public GridColumn Column { get; set; }
        private FormColumnViewModel _formColumn = null;
        public GridColumnViewModel(GridColumn column) : base(column)
        {
            Column = column;
        }

        public bool Editable => Column.Editable;
        public bool Sortable => Column.Sortable;
        public string FilterError => Column.FilterError;
        public FormColumnViewModel FormColumn 
        {
            get
            {
                if (_formColumn == null && Column.FormColumn != null)
                {
                    _formColumn = new FormColumnViewModel(Column.FormColumn);
                }
                return _formColumn;
            }
        }
        public List<bool> LineInError => Column.LineInError;
        public Image Image => Column.Image;
        public string Style => Column.Style;

        public object FormatValue(object value)
        {
            return Column.FormatValue(value);
        }
        public string TruncateValue(string value)
        {
            return Column.TruncateValue(value);
        }
        public string AlignmentClassName()
        {
            if (Column.DataType == typeof(Boolean) && (EnumOptions?.Any() ?? false) == false)
            {
                return "text-center";
            }
            else if (LookupOptions?.Any() ?? false) 
            {
                if (string.IsNullOrEmpty(Column.Lookup?.TableName) && EnumOptions == null && IsNumeric)
                {
                    return "text-right";
                }
            }
            else if (IsNumeric)
            {
                return "text-right";
            }

            return string.Empty;
        }
    }
}
