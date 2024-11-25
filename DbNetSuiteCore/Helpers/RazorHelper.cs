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
            List<string> dataAttributes = new List<string>();

            foreach (SelectColumn selectColumn in selectModel.Columns)
            {
                DataColumn? dataColumn = selectModel.GetDataColumn(selectColumn);
                if (dataColumn != null)
                {
                    dataAttributes.Add($"data-{dataColumn.ColumnName.ToLower()}=\"{HtmlEncoder.Default.Encode(selectColumn.FormatValue(row[dataColumn])?.ToString() ?? string.Empty)}\"");
                }
            }

            return new HtmlString(string.Join(" ", dataAttributes.ToArray()));
        }

        public static HtmlString Attribute(string name, object value)
        {
            return new HtmlString($"{name}=\"{HtmlEncoder.Default.Encode(value?.ToString() ?? string.Empty)}\"");
        }
    }
}