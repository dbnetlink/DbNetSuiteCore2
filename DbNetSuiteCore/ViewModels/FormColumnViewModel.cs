using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Models;
using DocumentFormat.OpenXml.Vml.Spreadsheet;

namespace DbNetSuiteCore.ViewModels
{
    public class FormColumnViewModel : ColumnViewModel
    {
        public FormColumn Column { get; set; }
        public FormColumnViewModel(FormColumn column) : base(column)
        {
            Column = column;
        }

        public bool InError
        {
            get
            {
                return Column.InError;
            }
            set
            {
                Column.InError = value;
            }
        }

        public object? FormatValue(object value)
        {
            return Column.FormatValue(value);
        }

        public object? RenderControl(string value, string dbValue, ComponentModel componentModel, int? rowIndex = null)
        {
            return Column.RenderControl(value, dbValue, componentModel, rowIndex);
        }
    }
}
