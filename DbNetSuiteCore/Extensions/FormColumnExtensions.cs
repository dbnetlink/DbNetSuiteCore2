using DbNetSuiteCore.Models;
using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.Extensions
{
    public static class FormColumnExtensions
    {
        public static object? FormatValue(this FormColumn formColumn, object value)
        {
            if (string.IsNullOrEmpty(value?.ToString()) || value == DBNull.Value)
            {
                return value;
            }

            if (formColumn?.DataType == typeof(DateTime) && string.IsNullOrEmpty(formColumn.Format))
            {
                formColumn.Format = "yyyy-MM-dd";
            }

            if (string.IsNullOrEmpty(formColumn.Format))
            {
                return value;
            }

            string format = formColumn.Format;

            return ColumnModelHelper.FormatedValue(formColumn, value);
        }
    }
}
