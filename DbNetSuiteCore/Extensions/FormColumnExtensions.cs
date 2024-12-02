using DbNetSuiteCore.Enums;
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

            switch (formColumn?.DataType.Name)
            {
                case nameof(DateTime):
                    return ((DateTime)value).ToString(formColumn.DateTimeFormat);
                case nameof(DateTimeOffset):
                    return ((DateTimeOffset)value).ToString(formColumn.DateTimeFormat);
                case nameof(TimeSpan):
                    return ((TimeSpan)value).ToString(formColumn.DateTimeFormat);
            }

            return value;
        }
    }
}
