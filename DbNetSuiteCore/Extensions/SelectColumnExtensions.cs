using DbNetSuiteCore.Models;
using DbNetSuiteCore.Constants;
using System.Text.RegularExpressions;
using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.Extensions
{
    public static class SelectColumnExtensions
    {
        public static object FormatValue(this SelectColumn selectColumn, object value)
        {
            if (string.IsNullOrEmpty(value?.ToString()) || value == DBNull.Value)
            {
                return value;
            }

            if (value is Byte[] byteValue)
            {
                try
                {
                    return Convert.ToBase64String(byteValue);
                }
                catch
                {
                    return value.ToString();
                }
            }

            if (selectColumn.DataType == typeof(DateTime) && string.IsNullOrEmpty(selectColumn.Format))
            {
                selectColumn.Format = "d";
            }

            if (string.IsNullOrEmpty(selectColumn.Format))
            {
                return value;
            }

            return ColumnModelHelper.FormatValue(selectColumn, value);
        }

    }
}
