using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using System.Globalization;

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
                    if (value is string)
                    {
                        DateTime dateTime;
                        if (DateTime.TryParse(value.ToString(), out dateTime) == false)
                        {
                            value = dateTime;
                        }
                        else if (DateTimeTryParseExact(value.ToString(), out dateTime))
                        {
                            value = dateTime;
                        }
                        else
                        {
                            return value;
                        }
                    }
                    return ((DateTime)value).ToString(formColumn.DateTimeFormat); ;
                case nameof(DateTimeOffset):
                    DateTimeOffset dateTimeOffset;
                    if (value is string)
                    {
                        if (DateTimeOffset.TryParse(value.ToString(), out dateTimeOffset))
                        {
                            value = dateTimeOffset;
                        }
                        else if (DateTimeOffsetTryParseExact(value.ToString(), out dateTimeOffset))
                        {
                            value = dateTimeOffset;
                        }
                        else
                        {
                            return value;
                        }
                    }
                    return ((DateTimeOffset)value).ToString(formColumn.DateTimeFormat);
                case nameof(TimeSpan):
                    TimeSpan timeSpan;
                    if (value is string)
                    {
                        if (TimeSpan.TryParse(value.ToString(), out timeSpan) == false)
                        {
                            return value;
                        }
                        value = timeSpan;
                    }
                    return ((TimeSpan)value).ToString(formColumn.DateTimeFormat);
                default:
                    if (string.IsNullOrEmpty(formColumn.Format) == false)
                    {
                        return ColumnModelHelper.FormatedValue(formColumn, value);
                    }
                    break;
            }

            return value;
        }


        private static bool DateTimeTryParseExact(string value, out DateTime dateTime)
        {
            List<string> formats = new List<string>() { "yyyy-MM-dd", "ddd MMM dd HH:mm:ss UTC yyyy", "yyyy-MM-dd HH:mm:ss" };

            foreach (string format in formats)
            {
                if (value.Length >= format.Length)
                {
                    if (DateTime.TryParseExact(value.ToString().Substring(0, format.Length), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
                    {
                        return true;
                    }
                }
            }

            dateTime = DateTime.Now;
            return false;
        }

        private static bool DateTimeOffsetTryParseExact(string value, out DateTimeOffset dateTimeOffset)
        {
            List<string> formats = new List<string>() { "yyyy-MM-dd", "ddd MMM dd HH:mm:ss UTC yyyy", "yyyy-MM-dd HH:mm:ss" };

            foreach (string format in formats)
            {
                if (value.Length >= format.Length)
                {
                    if (DateTimeOffset.TryParseExact(value.ToString().Substring(0, format.Length), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTimeOffset))
                    {
                        return true;
                    }
                }
            }

            dateTimeOffset = DateTime.Now;
            return false;
        }
    }
}

