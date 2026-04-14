using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Html;
using Newtonsoft.Json;
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
                //      dataAttributes.Add($"data-id=\"{TextHelper.ObfuscateString(JsonConvert.SerializeObject(gridModel.PrimaryKeyValue(row)), gridModel.HttpContext)}\"");
                dataAttributes.Add($"data-id=\"{gridModel.PrimaryKeyValue(row)}\"");
            }
            foreach (var column in gridModel.DataOnlyColumns)
            {
                string value = row[column.ColumnName]?.ToString() ?? string.Empty;
                string quote = value.Contains(@"""") ? "'" : @"""";
                dataAttributes.Add($"data-{column.ColumnName.ToLower()}={quote}{row[column.ColumnName]}{quote}");
            }

            return new HtmlString(string.Join(" ", dataAttributes.ToArray()));
        }

        public static HtmlString DataAttributes(DataRow row, SelectModel selectModel)
        {
            List<HtmlString> dataAttributes = new List<HtmlString>();

            foreach (SelectColumn selectColumn in selectModel.Columns)
            {
                DataColumn dataColumn = selectModel.GetDataColumn(selectColumn);
                if (dataColumn != null)
                {
                    dataAttributes.Add(Attribute($"data-{dataColumn.ColumnName.ToLower()}", selectColumn.FormatValue(row[dataColumn])?.ToString() ?? string.Empty));
                }
            }

            return new HtmlString(string.Join(" ", dataAttributes));
        }

        public static HtmlString Attributes(Dictionary<string, string> attributes)
        {
            attributes.Keys.ToList().ForEach(k => { attributes[k] = attributes[k]; });
            return new HtmlString(string.Join(" ", attributes.Keys.ToList().Select(key => Attribute(key, attributes[key])).ToList()));
        }

        public static HtmlString FormClassAttribute(string controlType, string className)
        {
            List<string> classNames = new List<string>() { "dbnetsuite", $"dbnetsuite-{controlType}" };
            if (string.IsNullOrEmpty(className) == false)
            {
                classNames.Add(className);
            }

            return Attribute("class", string.Join(" ", classNames));
        }

        public static HtmlString Attribute(string name, object value)
        {
            var attrValue = value?.ToString() ?? string.Empty;
            return new HtmlString($"{name}=\"{HtmlEncoder.Default.Encode(attrValue)}\"");
        }

        public static double? JavaScriptDateTime(object dateTime)
        {
            if (string.IsNullOrEmpty(dateTime?.ToString()) == false)
            {
                dateTime = Convert.ToDateTime(dateTime);
                if (dateTime is DateTime dt)
                {
                    return dt.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
                }
            }
            return null;
        }

        public static HtmlString IconButton(string type, HtmlString icon, Dictionary<string, string> attributes = null)
        {
            if (attributes == null)
            {
                attributes = new Dictionary<string, string>();
            }
            attributes.Add("button-type", type.ToLower());
            attributes.Add("type", "button");
            attributes.Add("title", ResourceHelper.GetResourceString(type));
            return new HtmlString($"<button {Attributes(attributes)}>{icon.ToString()}</button>");
        }

        public static HtmlString ModelState(ComponentModel componentModel, IConfiguration configuration)
        {
            var attributes = new Dictionary<string, string>();
            if (ConfigurationHelper.UseDistributedServerCache(configuration))
            {
                attributes.Add("value", CacheHelper.RedisCacheModel(componentModel));
            }
            else if (ConfigurationHelper.ServerStateManagement(configuration))
            {
                attributes.Add("value", CacheHelper.CacheModel(componentModel));
            }
            else
            {
                attributes.Add("value", TextHelper.ObfuscateString(componentModel));
            }
            attributes.Add("type", "hidden");
            attributes.Add("name", "model");
            return new HtmlString($"<input {Attributes(attributes)}/>");
        }

        public static HtmlString ModelSummaryState(ComponentModel componentModel, IConfiguration configuration)
        {
            var attributes = new Dictionary<string, string>();

            if (componentModel.SummaryModel is SummaryModel summaryModel)
            {
                if (ConfigurationHelper.UseDistributedServerCache(configuration))
                {
                    attributes.Add("value", CacheHelper.RedisCacheSummaryModel(summaryModel, componentModel));
                }
                else if (ConfigurationHelper.ServerStateManagement(configuration))
                {
                    attributes.Add("value", CacheHelper.CacheSummaryModel(summaryModel, componentModel));
                }
                else
                {
                    attributes.Add("value", TextHelper.ObfuscateString(summaryModel));
                }
            }
            attributes.Add("type", "hidden");
            attributes.Add("name", "summarymodel");
            return new HtmlString($"<input {Attributes(attributes)}/>");
        }
    }
}
