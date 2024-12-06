using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Html;
using System.Data;
using System.Text.Encodings.Web;

namespace DbNetSuiteCore.Helpers
{
    public static class RazorHelper
    {
        public static HtmlString CellDataAttributes(List<string> classes, object value, string style)
        {
            List<string> dataAttributes = new List<string>();

            if (classes.Any())
            {
                dataAttributes.Add($"class=\"{string.Join(" ", classes)}\"");
            }

            if (value != null && (value is byte) == false)
            {
                dataAttributes.Add($"data-value=\"{HtmlEncoder.Default.Encode(value?.ToString() ?? string.Empty)}\"");
            }

            if (string.IsNullOrEmpty(style) == false)
            {
                dataAttributes.Add($"style=\"{HtmlEncoder.Default.Encode(style)}\"");
            }

            return new HtmlString(string.Join(" ", dataAttributes.ToArray()));
        }

        public static HtmlString RowDataAttributes(DataRow row, GridModel gridModel)
        {
            List<string> dataAttributes = new List<string>();

            if (gridModel.PrimaryKeyValue(row) != null)
            {
                dataAttributes.Add($"data-id=\"{gridModel.PrimaryKeyValue(row)}\"");
            }

            foreach (var column in gridModel.DataOnlyColumns)
            {
                dataAttributes.Add($"data-{column.ColumnName.ToLower()}=\"{row[column.ColumnName]}\"");
            }

            return new HtmlString(string.Join(" ", dataAttributes.ToArray()));
        }

        public static HtmlString DataAttributes(DataRow row, SelectModel selectModel)
        {
            List<HtmlString> dataAttributes = new List<HtmlString>();

            foreach (SelectColumn selectColumn in selectModel.Columns)
            {
                DataColumn? dataColumn = selectModel.GetDataColumn(selectColumn);
                if (dataColumn != null)
                {
                    dataAttributes.Add(Attribute($"data-{dataColumn.ColumnName.ToLower()}", selectColumn.FormatValue(row[dataColumn])?.ToString() ?? string.Empty));
                }
            }

            return new HtmlString(string.Join(" ", dataAttributes));
        }

        public static HtmlString Attributes(Dictionary<string, string> attributes )
        {
            attributes.Keys.ToList().ForEach(k => { attributes[k] = attributes[k]; });
            return new HtmlString(string.Join(" ", attributes.Keys.ToList().Select(key => Attribute(key, attributes[key])).ToList()));
        }

        public static HtmlString Attribute(string name, object value)
        {
            var attrValue = value?.ToString() ?? string.Empty;
            switch (name)
            {
              //  case "data-value":
               //     break;
                default:
                    attrValue = HtmlEncoder.Default.Encode(attrValue);
                    break;
            }
            return new HtmlString($"{name}=\"{attrValue}\"");
        }


        public static double? JavaScriptDateTime(object? dateTime)
        {
            if (string.IsNullOrEmpty(dateTime?.ToString()))
                return null;

            dateTime = Convert.ToDateTime(dateTime);
            return (dateTime as DateTime?).Value.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
        }
    }
}