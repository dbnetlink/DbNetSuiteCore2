using DbNetSuiteCore.Constants;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using System;
using System.Text.RegularExpressions;

namespace DbNetSuiteCore.Extensions
{
    public static class GridColumnExtensions
    {
        internal static object FormatValue(this GridColumn gridColumn, object value)
        {
            if (string.IsNullOrEmpty(value?.ToString()) || value == DBNull.Value || gridColumn.NoFormat)
            {
                return value;
            }

            if (gridColumn?.DataType == typeof(DateTime) && string.IsNullOrEmpty(gridColumn.Format))
            {
                gridColumn.Format = "d";
            }
       
            if (string.IsNullOrEmpty(gridColumn?.Format))
            {
                return value;
            }

            if (string.IsNullOrEmpty(gridColumn.RegularExpression) == false)
            {
                value = Regex.Match(value.ToString() ?? string.Empty, gridColumn.RegularExpression);
            }

            string format = gridColumn.Format;

            switch (format)
            {
                case FormatType.Email:
                    if (ValidationHelper.IsValidEmail(value.ToString() ?? string.Empty))
                    {
                        value = $"<a href=\"mailto:{value}\">{value}</a>";
                    }
                    break;
                case FormatType.Url:
                    string uris = (value?.ToString() ?? string.Empty);
                    value = FormatUrls(uris);
                    break;
                case FormatType.Image:
                    value = string.Join("",value.ToString()!.Split(',').ToList().Select(s => $"<img {(string.IsNullOrEmpty(gridColumn.Style) ? "" : $"style=\"{gridColumn.Style}\"")} src =\"{s}\"/>"));
                    break;
                default:
                    try
                    {
                        value = ColumnModelHelper.FormatValue(gridColumn, value);
                    }
                    catch { }
                    break;
            }

            return value;
        }

        private static string FormatUrls(string uris)
        {
            List<string> links = new List<string>();
            foreach (string uri in uris.Split(","))
            {
                if (ValidationHelper.IsValidUri(uri))
                {
                    string text = uri.Split("?").First().Split("/").Last();
                    if (string.IsNullOrEmpty(text))
                    {
                        text = uri;
                    }
                    links.Add($"<a target=\"_blank\" href=\"{uri}\">{text}</a>");
                }
            }
            return string.Join(",",links);
        }
        internal static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            return DateTime.UnixEpoch.AddSeconds(unixTimeStamp).ToLocalTime();
        }

        internal static string TruncateValue(this GridColumn gridColumn, string value)
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
