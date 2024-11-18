using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.Helpers
{
    public static class ColumnModelHelper
    {

        public static object? TypedValue(ColumnModel columnModel, object value)
        {
            return TypedValue(columnModel?.DataType.Name, value);
        }

        public static object? TypedValue(string dataTypeName, object value)
        {
            switch (dataTypeName)
            {
                case nameof(DateTime):
                    return Convert.ToDateTime(value);
                case nameof(Int16):
                    return Convert.ToInt16(value);
                case nameof(Int32):
                    return Convert.ToInt32(value);
                case nameof(Int64):
                case nameof(Int128):
                    return Convert.ToInt64(value);
                case nameof(Double):
                    return Convert.ToDouble(value);
                case nameof(Single):
                    return Convert.ToSingle(value);
                case nameof(Decimal):
                    return Convert.ToDecimal(value);
            }

            return value;
        }

        public static string FormatedValue(ColumnModel columnModel, object value)
        {
            string format = columnModel.Format;

            if (format.Contains("{0}"))
            {
                return String.Format(format, value.ToString());
            }

            switch (columnModel?.DataType.Name)
            {
                case nameof(DateTime):
                    return Convert.ToDateTime(value).ToString(format);
                case nameof(Int16):
                    return Convert.ToInt16(value).ToString(format);
                case nameof(Int32):
                    return Convert.ToInt32(value).ToString(format);
                case nameof(Int64):
                case nameof(Int128):
                    return Convert.ToInt64(value).ToString(format);
                case nameof(Double):
                    return Convert.ToDouble(value).ToString(format);
                case nameof(Single):
                    return Convert.ToSingle(value).ToString(format);
                case nameof(Decimal):
                    return Convert.ToDecimal(value).ToString(format);
                case nameof(String):
                    return String.Format(format, value);
            }

            return value?.ToString() ?? string.Empty;
        }
    }
}
