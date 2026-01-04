using System.Data;
using DbNetSuiteCore.Constants;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Html;

namespace DbNetSuiteCore.ViewModels
{
    public class ComponentViewModel
    {
        private ComponentModel _componentModel;
        public IEnumerable<DataColumn> DataColumns => _componentModel.Data.Columns.Cast<DataColumn>();

        public string SubmitUrl => _componentModel.PostUrl;
        public string Diagnostics { get; set; } = string.Empty;
        public ComponentModel ComponentModel => _componentModel;

        public ComponentViewModel(ComponentModel componentModel)
        {
            _componentModel = componentModel;
            if (componentModel is GridModel)
            {
                HxIdTarget = $"#tbody{componentModel.Id}";
            }
            else
            {
                HxIdTarget = $"#hx_target_{componentModel.Id}";
            }
        }

        protected ColumnModel _GetColumnInfo(DataColumn column, IEnumerable<ColumnModel> columns)
        {
            return columns.FirstOrDefault(c => c.Name == column.ColumnName || c.Name.Split(".").Last() == column.ColumnName);
        }

        public DataColumn GetDataColumn(ColumnModel column)
        {
            return _componentModel.GetDataColumn(column);
        }

        public bool IsFolder(DataRow dataRow)
        {
            return Convert.ToBoolean(_componentModel.RowValue(dataRow, FileSystemColumn.IsDirectory.ToString(), false));
        }

        public string HxIdTarget { get; set; }
        public bool IsBlazor => _componentModel.IsBlazor;
        public bool IsParent => _componentModel.IsParent;
        public string PostUrl => _componentModel.PostUrl;
        public string HxFormTrigger => _componentModel.HxFormTrigger;
        public string Id => _componentModel.Id;
        public bool IsLinked => _componentModel.IsLinked;
        public string Message
        {
            get { return _componentModel.Message; }
            set { _componentModel.Message = value; }
        } 
        public MessageType MessageType => _componentModel.MessageType;
        public string JSON => _componentModel.JSON;
        public bool ValidationPassed => _componentModel.ValidationPassed;
        public string SearchDialogId => $"searchDialog{_componentModel.Id}";
        public string LookupDialogId => $"lookupDialog{_componentModel.Id}";

        public string LinkedControlIds => string.Join(",", _componentModel.GetLinkedControlIds());
        public IEnumerable<ColumnViewModel> SearchDialogColumns => _componentModel.GetColumns().Where(c => c.IsSearchable).Select(c => new ColumnViewModel(c));
        public bool SearchDialog => SearchDialogColumns.Any() && _componentModel.Search && _componentModel.DataSourceType != DataSourceType.MongoDB;
        public HtmlString RenderSearchLookupOptions(List<KeyValuePair<string, string>> options, string key)
        {
            List<HtmlString> html = new List<HtmlString>();
            html.Add(new HtmlString($"<select style=\"display:none\" data-key=\"{key}\">"));
            AddLookupFilterOptions(html, options, false);
            html.Add(new HtmlString($"</select>"));
            return new HtmlString(string.Join(" ", html));
        }

        protected void AddLookupFilterOptions(List<HtmlString> html, List<KeyValuePair<string, string>> options, bool includeEmpty = true)
        {
            if (includeEmpty)
            {
                html.Add(new HtmlString($"<option value=\"\"></option>"));
            }

            foreach (var option in options)
            {
                html.Add(new HtmlString($"<option value=\"{option.Key}\">{option.Value}</option>"));
            }
        }

        public HtmlString RenderLookupOptions(List<KeyValuePair<string, string>> options, string key)
        {
            List<HtmlString> html = new List<HtmlString>();
            html.Add(new HtmlString($"<select data-key=\"{key}\">"));
            AddLookupFilterOptions(html, options);
            html.Add(new HtmlString($"</select>"));
            return new HtmlString(string.Join(" ", html));
        }

        protected string ButtonText(ResourceNames resourceName)
        {
            //    return $"{ResourceHelper.GetResourceString(resourceName)} {ResourceHelper.GetResourceString(ResourceNames.Record).ToLower()}";
            return $"{ResourceHelper.GetResourceString(resourceName)}";
        }
    }
}
