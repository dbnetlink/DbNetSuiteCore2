using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using System.Globalization;

namespace DbNetSuiteCore.Extensions
{
    public static class FormColumnExtensions
    {
        public static object FormatValue(this FormColumn formColumn, object value)
        {
            if (string.IsNullOrEmpty(value?.ToString()) || value == DBNull.Value)
            {
                return value;
            }

            if (value is string stringValue)
            {
                switch (formColumn?.DataType.Name)
                {
                    case nameof(DateTime):
                        DateTime dateTime;
                        if (DateTime.TryParse(stringValue, out dateTime) == false)
                        {
                            value = dateTime;
                        }
                        else if (DateTimeTryParseExact(stringValue, out dateTime))
                        {
                            value = dateTime;
                        }
                        else
                        {
                            return value;
                        }
                        break;
                    case nameof(DateTimeOffset):
                        DateTimeOffset dateTimeOffset;

                        if (DateTimeOffset.TryParse(stringValue, out dateTimeOffset))
                        {
                            value = dateTimeOffset;
                        }
                        else if (DateTimeOffsetTryParseExact(stringValue, out dateTimeOffset))
                        {
                            value = dateTimeOffset;
                        }
                        else
                        {
                            return value;
                        }
                        break;
                    case nameof(TimeSpan):
                        TimeSpan timeSpan;
                        if (TimeSpan.TryParse(stringValue, out timeSpan) == false)
                        {
                            return value;
                        }
                        value = timeSpan;
                        break;
                }
            }

            switch (formColumn?.DataType.Name)
            {
                case nameof(DateTime):
                    return ((DateTime)value).ToString(formColumn.DateTimeFormat); ;
                case nameof(DateTimeOffset):
                    return ((DateTimeOffset)value).ToString(formColumn.DateTimeFormat);
                case nameof(TimeSpan):
                    return ((TimeSpan)value).ToString(formColumn.DateTimeFormat);
            }

            if (string.IsNullOrEmpty(formColumn?.Format) == false)
            {
                var formattedValue = ColumnModelHelper.FormatValue(formColumn, value);

                if (formColumn.ControlType == Enums.FormControlType.Number || formColumn.MinValue != null || formColumn.MaxValue != null)
                {
                    formattedValue = RemoveNonNumberDigitsAndCharacters(formattedValue);
                }

                return formattedValue;
            }
            return value;
        }

        private static string RemoveNonNumberDigitsAndCharacters(string text)
        {
            var numericChars = "0123456789,.".ToCharArray();
            return new String(text.Where(c => numericChars.Any(n => n == c)).ToArray());
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

