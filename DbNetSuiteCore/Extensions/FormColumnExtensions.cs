using DbNetSuiteCore.Models;

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

            if (formColumn?.DataType == typeof(DateTime))
            {
                return ((DateTime)value).ToString("yyyy-MM-dd");
            }
            if (formColumn?.DataType == typeof(DateTimeOffset))
            {
                return ((DateTimeOffset)value).ToString("yyyy-MM-dd");
            }
            if (formColumn?.DataType == typeof(TimeSpan))
            {
                return ((TimeSpan)value).ToString("g");
            }
            return value;
        }
    }
}
