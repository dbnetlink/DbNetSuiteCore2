using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Models;
using DocumentFormat.OpenXml.Vml.Spreadsheet;
using Microsoft.AspNetCore.Html;

namespace DbNetSuiteCore.ViewModels
{
    public class FormColumnViewModel : ColumnViewModel
    {
        public FormColumn Column { get; set; }
        public FormColumnViewModel(FormColumn column) : base(column)
        {
            Column = column;
        }

        public bool DataOnly => Column.DataOnly;
        public bool Autoincrement => Column.Autoincrement;

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

        public HtmlString RenderLabel(FormModel formModel)
        {
            return Column.RenderLabel(formModel);
        }
    }
}
