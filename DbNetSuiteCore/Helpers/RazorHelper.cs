﻿using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Html;
using System.Data;

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
                dataAttributes.Add($"data-value=\"{value}\"");
            }

            if (string.IsNullOrEmpty(style) == false)
            {
                dataAttributes.Add($"style=\"{style}\"");
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
    }
}
