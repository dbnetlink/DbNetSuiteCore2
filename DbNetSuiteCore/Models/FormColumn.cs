using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Helpers;
using DocumentFormat.OpenXml.Drawing;
using Microsoft.AspNetCore.Html;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Data;
using System.Text.RegularExpressions;

namespace DbNetSuiteCore.Models
{
    public class FormColumn : ColumnModel
    {
        private object? _minValue { get; set; } = null;
        private object? _maxValue { get; set; } = null;
        public FormControlType ControlType { get; set; } = FormControlType.Auto;
        public bool Required { get; set; } = false;
        public bool InError { get; set; } = false;
        public bool ReadOnly { get; set; } = false;
        public bool Disabled { get; set; } = false;
        public object? InitialValue { get; set; } = null;
        public object? MinValue
        {
            get
            {
                if (_minValue != null)
                {
                    return _minValue;
                }
                switch (DbDataType)
                {
                    case nameof(MSSQLDataTypes.TinyInt):
                        _minValue = 0;
                        break;
                }
                return _minValue;
            }
            set { _minValue = value; }
        }
        public object? MaxValue
        {
            get
            {
                if (_maxValue != null)
                {
                    return _maxValue;
                }
                switch (DbDataType)
                {
                    case nameof(MSSQLDataTypes.TinyInt):
                        _maxValue = 255;
                        break;
                }
                return _maxValue;
            }
            set { _maxValue = value; }
        }
        public bool Autoincrement { get; set; } = false;
        public TextTransform? TextTransform { get; set; } = null;
        public int? MaxLength { get; set; } = null;
        public int? MinLength { get; set; } = null;
        public bool PrimaryKeyRequired => PrimaryKey && Autoincrement == false;
        public string DateTimeFormat => GetDateTimeFormat();
        public int ColSpan { get; set; } = 1;
        public int RowSpan { get; set; } = 1;
        public int TextAreaRows { get; set; } = 4;
        public string HelpText { get; set; } = string.Empty;
        public int Size { get; set; } = 4;
        public string Style { get; set; } = string.Empty;
        public string? Pattern { get; set; } = null;
        public int? Step { get; set; } = null;

        public bool SelectControlType => ControlType == FormControlType.Auto || ControlType == FormControlType.SelectMultiple;

        public HtmlEditor? HtmlEditor { get; set; } = null;
        public FormColumn()
        {
        }
        public FormColumn(string name) : base(name, TextHelper.GenerateLabel(name))
        {
        }

        public FormColumn(string expression, string label) : base(expression, label)
        {
        }

        internal FormColumn(DataColumn dataColumn, DataSourceType dataSourceType) : base(dataColumn, dataSourceType)
        {
        }

        internal FormColumn(DataRow dataRow, DataSourceType dataSourceType) : base(dataRow, dataSourceType)
        {
        }

        internal FormColumn(BsonElement element) : base(element)
        {
        }

        private string GetDateTimeFormat()
        {
            switch (DataType.Name)
            {
                case nameof(DateTime):
                    return (ControlType == FormControlType.DateTime) ? "yyyy-MM-dd'T'HH:mm" : "yyyy-MM-dd";
                case nameof(DateTimeOffset):
                    return (ControlType == FormControlType.DateTime) ? "yyyy-MM-dd'T'HH:mm" : "yyyy-MM-dd";
                case nameof(TimeSpan):
                    return (ControlType == FormControlType.TimeWithSeconds) ? @"hh\:mm\:ss" : @"hh\:mm";
            }

            return string.Empty;
        }

        public HtmlString RenderLabel()
        {
            return new HtmlString($"<label for=\"{ColumnName}\" class=\"font-bold text-slate-800\">{Label}</label>");
        }

        public HtmlString RenderControl(string value, string dbValue, FormModel formModel)
        {
            var attributes = new Dictionary<string, string>();

            switch (ControlType)
            {
                case FormControlType.Text:
                case FormControlType.Number:
                case FormControlType.Email:
                case FormControlType.Url:
                case FormControlType.Color:
                case FormControlType.Password:
                    attributes["type"] = ControlType.ToString().ToLower();
                    break;
                case FormControlType.DateTime:
                    attributes["type"] = "datetime-local";
                    break;
                default:
                    switch (DataTypeName)
                    {
                        case nameof(DateTime):
                        case nameof(DateTimeOffset):
                            attributes["type"] = "date";
                            break;
                        case nameof(TimeSpan):
                            attributes["type"] = "time";
                            break;
                        default:
                            attributes["type"] = "text";
                            break;
                    }
                    break;
            }

            attributes["id"] = $"{formModel.Id}_{ColumnName}";
            attributes["name"] = $"_{ColumnName}";
            attributes["data-value"] = $"{dbValue}";
            attributes["value"] = $"{value}";
            attributes["data-datatype"] = DataTypeName;
            attributes["data-dbdatatype"] = DbDataType;
            attributes["class"] = Classes(formModel);
            attributes["data-error"] = InError.ToString().ToLower();

            if (string.IsNullOrEmpty(Style) == false)
            {
                attributes["style"] = Style;
            }

            if (Step.HasValue)
            {
                attributes["step"] = Step.Value.ToString();
            }

            switch (ControlType)
            {
                case FormControlType.Time:
                case FormControlType.TimeWithSeconds:
                    attributes["type"] = "time";
                    if (ControlType == FormControlType.TimeWithSeconds)
                    {
                        attributes["step"] = "1";
                    }
                    break;
                case FormControlType.Auto:
                    if (dbValue.Contains(Environment.NewLine))
                    {
                        ControlType = FormControlType.TextArea;
                    }
                    break;
            }

            SetMinMax(attributes, "min", MinValue);
            SetMinMax(attributes, "max", MaxValue);

            if (DataOnly)
            {
                attributes["type"] = "hidden";
                return RenderInput(attributes, formModel);
            }
            else if (LookupOptions != null && SelectControlType)
            {
                return RenderSelect(value, attributes, formModel);
            }
            else if (ControlType == FormControlType.TextArea || HtmlEditor.HasValue)
            {
                return RenderTextArea(value, attributes, formModel);
            }
            else if (DataType == typeof(Boolean))
            {
                return RenderCheckbox(value, attributes, formModel);
            }
            else
            {
                return RenderInput(attributes, formModel);
            }
        }

        private string HelpTextElement()
        {
            return string.IsNullOrEmpty(HelpText) ? string.Empty : $"<small>{HelpText}</small>";
        }

        public string ToStringOrEmpty(object? value)
        {
            return value?.ToString() ?? string.Empty;
        }

        private HtmlString RenderInput(Dictionary<string, string> attributes, FormModel formModel)
        {
            AddAttribute(attributes, TextTransform, "data-texttransform");
            AddAttribute(attributes, MaxLength, "maxlength");
            AddAttribute(attributes, MinLength, "minlength");

            List<string> dataList = new List<string>();
            if (LookupOptions != null)
            {
                attributes["list"] = $"{attributes["id"]}_datalist";
                dataList.Add($"<datalist id=\"{attributes["list"]}\">");
                dataList.AddRange(OptionsList());
                dataList.Add($"</datalist>{HelpTextElement()}");
            }
            return new HtmlString($"<input {RazorHelper.Attributes(attributes)} {Attributes(formModel)} />{HelpTextElement()}{string.Join(string.Empty,dataList)}");
        }

        private void AddAttribute(Dictionary<string, string> attributes, object? attrValue , string attrName)
        {
            if (DataType == typeof(string) && attrValue != null)
            {
                attributes[attrName] = ToStringOrEmpty(attrValue);
            }
        }
        private HtmlString RenderSelect(string value, Dictionary<string, string> attributes, FormModel formModel)
        {
            if (DataType == typeof(Boolean))
            {
                value = ComponentModelExtensions.ParseBoolean(value) ? "1" : "0";
            }

            List<string> values = new List<string>() { value };

            switch (DbDataType)
            {
                case nameof(MySqlDataTypes.Set):
                case nameof(BsonType.Array):
                    ControlType = FormControlType.SelectMultiple;
                    break;
            }

            List<string> attr = new List<string>();
            if (ControlType == FormControlType.SelectMultiple)
            {
                attr.Add("multiple");
                values = value.Split(Environment.NewLine).ToList();
                if (values.Count == 1)
                {
                    values = value.Split(",").ToList();
                }
                attributes.Add("size", Size.ToString());
            }

            List<string> select = new List<string>();

            select.Add($"<select {RazorHelper.Attributes(attributes)} {Attributes(formModel, attr)}>");
            select.AddRange(OptionsList(values));
            select.Add($"</select>{HelpTextElement()}");

            return new HtmlString(String.Join(string.Empty, select));
        }

        private List<string> OptionsList(List<string>? values = null)
        {
            List<string> options = new List<string>();
            if (Required == false)
            {
                options.Add("<option value=\"\"></option >");
            }

            foreach (var option in LookupOptions ?? new List<KeyValuePair<string, string>>())
            {
                options.Add($"<option value=\"{option.Key}\" {((values ?? new List<string>()).Contains(option.Key) ? "selected" : "")}>{option.Value}</option>");
            }
            return options;
        }

        private HtmlString RenderTextArea(string value, Dictionary<string, string> attributes, FormModel formModel)
        {
            attributes.Remove("value");
            attributes["rows"] = TextAreaRows.ToString();
            string text = value;

            if (HtmlEditor.HasValue)
            {
                attributes["data-htmleditor"] = HtmlEditor.Value.ToString();
            }

            string textArea = $"<textarea {RazorHelper.Attributes(attributes)} {Attributes(formModel)}>{text}</textarea>";

            if (HtmlEditor.HasValue)
            {
                switch (HtmlEditor.Value)
                {
                    case Enums.HtmlEditor.Froala:
                    case Enums.HtmlEditor.CKEditor:
                        textArea = $"{textArea}<div class=\"hidden\" id=\"{attributes["id"]}_{HtmlEditor.Value.ToString().ToLower()}\">{text}</div>";
                        break;
                }
            }
            else
            {
                textArea = $"{textArea}{HelpTextElement()}";
            }
            return new HtmlString(textArea);
        }

        private HtmlString RenderCheckbox(string value, Dictionary<string, string> attributes, FormModel formModel)
        {
            attributes.Remove("value");
            attributes.Remove("data-error");
            attributes["class"] = CheckboxClasses(formModel);
            attributes["style"] = "transform: scale(1.5);margin-left:5px";

            bool boolValue = ComponentModelExtensions.ParseBoolean(value);

            string checkbox = $"<input type=\"checkbox\" {RazorHelper.Attributes(attributes)} {CheckboxAttributes(formModel, boolValue)}/>{HelpTextElement()}";

            return new HtmlString(checkbox);
        }

        string Classes(FormModel formModel)
        {
            List<string> classes = new List<string>() { "fc-control w-full" };
            if (IsNumeric && LookupOptions == null)
            {
                classes.Add("text-right");
            }

            if (ReadOnly || DataType == typeof(Guid) || formModel.ReadOnly)
            {
                classes.Add("readonly");
            }

            if (HtmlEditor.HasValue)
            {
                classes.Add("hidden");
            }

            return string.Join(" ", classes);
        }

        string CheckboxClasses(FormModel formModel)
        {
            List<string> classes = new List<string>() { "fc-control" };

            if (ReadOnly || formModel.ReadOnly)
            {
                classes.Add("readonly");
            }

            return string.Join(" ", classes);
        }

        string Attributes(FormModel formModel, List<string>? attr = null)
        {
            List<string> attributes = new List<string>();
            if (Disable(formModel))
            {
                attributes.Add("disabled");
            }
            if (Required)
            {
                attributes.Add("required");
            }
            if ((ReadOnly && LookupOptions == null) || formModel.ReadOnly)
            {
                attributes.Add("readonly");
            }

            if (attr != null)
            {
                attributes.AddRange(attr);
            }

            return string.Join(" ", attributes);
        }
        string CheckboxAttributes(FormModel formModel, bool boolValue)
        {
            List<string> attributes = new List<string>();
            if (Disabled || formModel.Mode == FormMode.Empty)
            {
                attributes.Add("disabled");
            }

            if (boolValue)
            {
                attributes.Add("checked");
            }

            return string.Join(" ", attributes);
        }

        bool Disable(FormModel formModel)
        {
            if (formModel.Mode == FormMode.Insert && PrimaryKeyRequired)
            {
                return false;
            }
            return (PrimaryKey || ForeignKey || Disabled || formModel.Mode == FormMode.Empty);
        }

        void SetMinMax(Dictionary<string, string> attributes, string attrName, object? attrValue)
        {
            if (attrValue == null)
            {
                return;
            }

            switch (attrValue.GetType().Name)
            {
                case nameof(Int64):
                    attributes["type"] = nameof(FormControlType.Number).ToLower();
                    attributes[attrName] = attrValue?.ToString() ?? String.Empty;
                    break;
                case nameof(DateTime):
                    attributes["type"] = nameof(FormControlType.Date).ToLower();
                    attributes[attrName] = ((DateTime)attrValue).ToString("yyyy-MM-dd");
                    break;
                default:
                    attributes[attrName] = attrValue?.ToString() ?? String.Empty;
                    break;
            }
        }

        public string GetInitialValue()
        {
            if (InitialValue == null)
            {
                return string.Empty;
            }

            switch (InitialValue.GetType().Name)
            {
                 case nameof(DateTime):
                    return ((DateTime)InitialValue).ToString("yyyy-MM-dd");
                default:
                    return this.FormatValue(InitialValue)?.ToString() ?? string.Empty;
            }
        }
    }
}