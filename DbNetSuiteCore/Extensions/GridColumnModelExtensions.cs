using DbNetSuiteCore.Models;
using DbNetSuiteCore.Constants;
using System.Text.RegularExpressions;

namespace DbNetSuiteCore.Extensions
{
    public static class GridColumnModelExtensions
    {
        public static object? FormatValue(this GridColumn gridColumnModel, object value)
        {
            if (string.IsNullOrEmpty(value?.ToString()) || value == DBNull.Value)
            {
                return value;
            }

            if (gridColumnModel?.DataType == typeof(DateTime) && string.IsNullOrEmpty(gridColumnModel.Format))
            {
                gridColumnModel.Format = "d";
            }

            if (string.IsNullOrEmpty(gridColumnModel.Format))
            {
                return value;
            }

            if (string.IsNullOrEmpty(gridColumnModel.RegularExpression) == false)
            {
                value = Regex.Match(value.ToString(), gridColumnModel.RegularExpression);
            }

            string format = gridColumnModel.Format;

            switch (format)
            {
                case FormatType.Email:
                    value = $"<a href=\"mailto:{value}\">{value}</a>";
                    break;
                case FormatType.Url:
                    value = $"<a target=\"_blank\" href=\"{value}\">{value}</a>";
                    break;
                case FormatType.Image:
                    value = string.Join("",value.ToString()!.Split(',').ToList().Select(s => $"<img {(string.IsNullOrEmpty(gridColumnModel.Style) ? "" : $"style=\"{gridColumnModel.Style}\"")} src =\"{s}\"/>"));
                    break;
                default:
                    value = gridColumnModel.FormatedValue(value, format);
                    break;
            }

            return value;
        }

        public static object? TypedValue(this ColumnModel columnModel, object value)
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

        public static string FormatedValue(this GridColumn gridColumnModel, object value, string format)
        {   
            if (format.Contains("{0}"))
            {
                return String.Format(format, value.ToString());
            }

            switch (gridColumnModel?.DataType.Name)
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
