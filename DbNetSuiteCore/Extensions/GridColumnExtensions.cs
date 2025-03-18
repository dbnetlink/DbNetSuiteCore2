using DbNetSuiteCore.Models;
using DbNetSuiteCore.Constants;
using System.Text.RegularExpressions;
using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.Extensions
{
    public static class GridColumnExtensions
    {
        public static object? FormatValue(this GridColumn gridColumn, object value)
        {
            if (string.IsNullOrEmpty(value?.ToString()) || value == DBNull.Value)
            {
                return value;
            }

            if (gridColumn?.DataType == typeof(DateTime) && string.IsNullOrEmpty(gridColumn.Format))
            {
                gridColumn.Format = "d";
            }

            if (string.IsNullOrEmpty(gridColumn.Format))
            {
                return value;
            }

            if (string.IsNullOrEmpty(gridColumn.RegularExpression) == false)
            {
                value = Regex.Match(value.ToString(), gridColumn.RegularExpression);
            }

            string format = gridColumn.Format;

            switch (format)
            {
                case FormatType.Email:
                    value = $"<a href=\"mailto:{value}\">{value}</a>";
                    break;
                case FormatType.Url:
                    value = $"<a target=\"_blank\" href=\"{value}\">{value}</a>";
                    break;
                case FormatType.Image:
                    value = string.Join("",value.ToString()!.Split(',').ToList().Select(s => $"<img {(string.IsNullOrEmpty(gridColumn.Style) ? "" : $"style=\"{gridColumn.Style}\"")} src =\"{s}\"/>"));
                    break;
                default:
                    value = ColumnModelHelper.FormatValue(gridColumn,value);
                    break;
            }

            return value;
        }

        public static string TruncateValue(this GridColumn gridColumn, string value)
        {
            var array = value.Substring(0, gridColumn.MaxChars).Split(" ");

            if (array.Length > 1) {
                value = string.Join(" ", array.Reverse().Skip(1).Reverse().ToArray());
            }
            else {
                value = array.First();
            }
            return $"{value}...";
        }
    }
}
