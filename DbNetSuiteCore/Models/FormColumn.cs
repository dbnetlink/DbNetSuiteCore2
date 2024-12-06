using DbNetSuiteCore.Constants;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Helpers;
using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Html;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Data;
using System.Text.Encodings.Web;

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
        public object InitialValue { get; set; } = string.Empty;
        public object? MinValue
        {
            get
            {
                if (_minValue != null)
                {
                    return _minValue;
                }
                switch (DbDataType.ToLower())
                {
                    case "tinyint":
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
                switch (DbDataType.ToLower())
                {
                    case "tinyint":
                        _maxValue = 255;
                        break;
                }
                return _maxValue;
            }
            set { _maxValue = value; }
        }
        public long? JSDateTime { get; set; } = null;
        public bool Autoincrement { get; set; } = false;
        public TextTransform? TextTransform { get; set; } = null;
        public int? MaxLength { get; set; } = null;
        public bool PrimaryKeyRequired => PrimaryKey && Autoincrement == false;

        public string DateTimeFormat => GetDateTimeFormat();

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
                case FormControlType.Number:
                case FormControlType.Email:
                case FormControlType.Url:
                case FormControlType.Color:
                case FormControlType.Password:
                case FormControlType.Range:
                case FormControlType.Tel:
                case FormControlType.Week:
                case FormControlType.Month:
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

            attributes["name"] = $"_{ColumnName}";
            attributes["data-value"] = $"{dbValue}";
            attributes["value"] = $"{value}";
            attributes["data-datatype"] = DataTypeName;
            attributes["data-dbdatatype"] = DbDataType;
            attributes["class"] = Classes(formModel);
            attributes["data-error"] = InError.ToString().ToLower();

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
                    string val = HtmlEncoder.Default.Encode(dbValue);
                    if (val.Contains("&#xA;"))
                    {
                        ControlType = FormControlType.TextArea;
                        attributes["data-value"] = HtmlEncoder.Default.Encode(val);
                    }
                    break;
            }

            if (DbDataType == "tinyint")
            {
                attributes["min"] = ToStringOrEmpty(MinValue);
                attributes["max"] = ToStringOrEmpty(MaxValue);
            }

            if (LookupOptions != null)
            {
                return RenderSelect(value, attributes, formModel);
            }
            else if (ControlType == FormControlType.TextArea)
            {
                return RenderTextArea(value, attributes, formModel);
            }
            else if (DataType == typeof(Boolean))
            {
                return RenderCheckbox(value, attributes, formModel);
            }
            else
            {
                if (TextTransform.HasValue)
                {
                    attributes["data-texttransform"] = ToStringOrEmpty(TextTransform);
                }
                switch (attributes["type"])
                {
                    case "text":
                        if (DataType == typeof(string))
                        {
                            attributes["maxlength"] = ToStringOrEmpty(MaxLength);
                        }
                        break;
                }

                return new HtmlString($"<input {RazorHelper.Attributes(attributes)} {Attributes(formModel)} />");
            }
        }

        private string ToStringOrEmpty(object? value)
        {
            return value?.ToString() ?? string.Empty;   
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
                    values = value.Split(",").ToList();
                    break;
                case nameof(BsonType.Array):
                    values = value.Split(Environment.NewLine).ToList();
                    break;
            }

            List<string> select = new List<string>();

            select.Add($"<select {RazorHelper.Attributes(attributes)} {Attributes(formModel)}>");
            select.Add("<option value=\"\"></option >");

            foreach(var option in LookupOptions)
            {
                select.Add($"<option value=\"{option.Key}\" {(values.Contains(option.Key) ? "selected" : "")}>{option.Value}</option>");
            }
            select.Add("</select>");

            return new HtmlString(String.Join(string.Empty,select));
        }

        private HtmlString RenderTextArea(string value, Dictionary<string, string> attributes, FormModel formModel)
        {
            attributes.Remove("value");
            string text = value;//.ToString().Replace(EncodedAscii.LineFeed, Environment.NewLine).Replace("&#xA;", Environment.NewLine);
            string textArea = $"<textarea {RazorHelper.Attributes(attributes)} {Attributes(formModel)}>{text}</textarea>";

            return new HtmlString(String.Join(string.Empty, textArea));
        }

        private HtmlString RenderCheckbox(string value, Dictionary<string, string> attributes, FormModel formModel)
        {
            attributes.Remove("value");
            attributes.Remove("data-error");
            attributes["class"] = CheckboxClasses(formModel);
            attributes["style"] = "transform: scale(1.5);margin-left:5px";

            bool boolValue = ComponentModelExtensions.ParseBoolean(value);

            string checkbox = $"<input type=\"checkbox\" {RazorHelper.Attributes(attributes)} {CheckboxAttributes(formModel, boolValue)}/>";

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

        string Attributes(FormModel formModel)
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

            if (LookupOptions != null)
            {
                switch (DbDataType)
                {
                    case nameof(MySqlDataTypes.Set):
                    case nameof(BsonType.Array):
                        attributes.Add("multiple");
                        attributes.Add(RazorHelper.Attribute("size", 4).ToString());
                        break;
                }
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
    }
}