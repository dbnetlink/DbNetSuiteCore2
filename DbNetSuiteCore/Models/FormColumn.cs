using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Extensions;
using DbNetSuiteCore.Helpers;
using Microsoft.AspNetCore.Html;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Data;

namespace DbNetSuiteCore.Models
{
    public class FormColumn : GridFormColumn
    {
        private bool _suggest = false;
        public ReadOnlyMode? ReadOnly { get; set; } = null;
        public bool Disabled { get; set; } = false;
        public object? InitialValue { get; set; } = null;
        public bool Autoincrement { get; set; } = false;
        public bool PrimaryKeyRequired => PrimaryKey && Autoincrement == false;
        public string DateTimeFormat => GetDateTimeFormat(ControlType.ToString());
        public int ColSpan { get; set; } = 1;
        public int RowSpan { get; set; } = 1;
        public int TextAreaRows { get; set; } = 4;
        public string HelpText { get; set; } = string.Empty;
        public int Size { get; set; } = 4;
        public string Style { get; set; } = string.Empty;
        public double? Step { get; set; } = null;
        public bool Suggest
        {
            get { return _suggest; }
            set
            {
                _suggest = value;
                if (value == true && Lookup == null)
                {
                    Lookup = new Lookup();
                }
            }
        }

        public bool HashPassword { get; set; } = false;
        public bool IsReadOnly(FormMode formMode)
        {
            if (ReadOnly.HasValue == false)
            {
                return false;
            }

            switch (ReadOnly.Value)
            {
                case ReadOnlyMode.InsertAndUpdate:
                    return true;
                case ReadOnlyMode.InsertOnly: 
                    return formMode == FormMode.Insert;
                case ReadOnlyMode.UpdateOnly:
                    return formMode == FormMode.Update;
            }

            return false;
        }

        public bool SelectControlType => (ControlType == FormControlType.Auto && Suggest == false) || ControlType == FormControlType.SelectMultiple;
        public string SequenceName { get; set; } = string.Empty;

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


        public HtmlString RenderLabel()
        {
            return new HtmlString($"<label for=\"{ColumnName}\" class=\"font-bold text-slate-800\">{Label}</label>");
        }

        public HtmlString RenderControl(string value, string dbValue, ComponentModel componentModel)
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

            attributes["id"] = $"{componentModel.Id}_{ColumnName}";
            attributes["name"] = $"_{ColumnName}";
            attributes["data-value"] = $"{dbValue}";
            attributes["value"] = $"{value}";
            attributes["data-datatype"] = DataTypeName;
            attributes["data-dbdatatype"] = DbDataType;
            attributes["class"] = Classes(componentModel);
            attributes["data-error"] = InError.ToString().ToLower();

            if (string.IsNullOrEmpty(Style) == false)
            {
                attributes["style"] = Style;
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

            if (ControlType == FormControlType.Number && Step.HasValue == false)
            {
                if (DataTypeName.StartsWith("Int") == false)
                {
                    Step = 0.01;
                }
            }

            if (Step.HasValue)
            {
                attributes["step"] = Step.Value.ToString("0.00");
            }

            if (DataOnly)
            {
                attributes["type"] = "hidden";
                return RenderInput(attributes, componentModel);
            }
            else if (LookupOptions != null && SelectControlType)
            {
                return RenderSelect(value, attributes, componentModel);
            }
            else if (ControlType == FormControlType.TextArea || HtmlEditor.HasValue)
            {
                return RenderTextArea(value, attributes, componentModel);
            }
            else if (DataType == typeof(Boolean))
            {
                return RenderCheckbox(value, attributes, componentModel);
            }
            else
            {
                return RenderInput(attributes, componentModel);
            }
        }

        private string HelpTextElement()
        {
            return string.IsNullOrEmpty(HelpText) ? string.Empty : $"<small>{HelpText}</small>";
        }

        private HtmlString RenderInput(Dictionary<string, string> attributes, ComponentModel componentModel)
        {
            AddAttribute(attributes, TextTransform, "data-texttransform");
            AddAttribute(attributes, MaxLength, "maxlength");
            AddAttribute(attributes, MinLength, "minlength");

            string dataList = DataList(attributes);
           
            return new HtmlString($"<input {RazorHelper.Attributes(attributes)} {Attributes(componentModel)} />{HelpTextElement()}{dataList}");
        }

        private void AddAttribute(Dictionary<string, string> attributes, object? attrValue , string attrName)
        {
            if (DataType == typeof(string) && attrValue != null)
            {
                attributes[attrName] = this.ToStringOrEmpty(attrValue);
            }
        }
        private HtmlString RenderSelect(string value, Dictionary<string, string> attributes, ComponentModel componentModel)
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

            select.Add($"<select {RazorHelper.Attributes(attributes)} {Attributes(componentModel, attr)}>");
            select.AddRange(OptionsList(values, false));
            select.Add($"</select>{HelpTextElement()}");

            return new HtmlString(String.Join(string.Empty, select));
        }

        private HtmlString RenderTextArea(string value, Dictionary<string, string> attributes, ComponentModel componentModel)
        {
            attributes.Remove("value");
            attributes["rows"] = TextAreaRows.ToString();
            string text = value;

            if (HtmlEditor.HasValue)
            {
                attributes["data-htmleditor"] = HtmlEditor.Value.ToString();
            }

            string textArea = $"<textarea {RazorHelper.Attributes(attributes)} {Attributes(componentModel)}>{text}</textarea>";

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

        private HtmlString RenderCheckbox(string value, Dictionary<string, string> attributes, ComponentModel componentModel)
        {
            attributes.Remove("value");
            attributes.Remove("data-error");
            attributes["class"] = CheckboxClasses(componentModel);
            attributes["style"] = "transform: scale(1.5);margin-left:5px";

            bool boolValue = ComponentModelExtensions.ParseBoolean(value);

            string checkbox = $"<input type=\"checkbox\" {RazorHelper.Attributes(attributes)} {CheckboxAttributes(componentModel, boolValue)}/>{HelpTextElement()}";

            return new HtmlString(checkbox);
        }

        string Classes(ComponentModel componentModel)
        {
            List<string> classes = new List<string>() { "fc-control w-full" };
            if (IsNumeric && LookupOptions == null)
            {
                classes.Add("text-right");
            }

            FormMode mode = componentModel is FormModel ? ((FormModel)componentModel).Mode : FormMode.Update;
            bool readOnly = componentModel is FormModel ? ((FormModel)componentModel).ReadOnly : false;

            if (IsReadOnly(mode) || DataType == typeof(Guid) || readOnly)
            {
                classes.Add("readonly");
            }

            if (HtmlEditor.HasValue)
            {
                classes.Add("hidden");
            }

            return string.Join(" ", classes);
        }

        string CheckboxClasses(ComponentModel componentModel)
        {
            List<string> classes = new List<string>() { "fc-control" };

            FormMode mode = componentModel is FormModel ? ((FormModel)componentModel).Mode : FormMode.Update;
            bool readOnly = componentModel is FormModel ? ((FormModel)componentModel).ReadOnly : false;

            if (IsReadOnly(mode) || readOnly)
            {
                classes.Add("readonly");
            }

            return string.Join(" ", classes);
        }

        string Attributes(ComponentModel componentModel, List<string>? attr = null)
        {
            List<string> attributes = new List<string>();
            if (Disable(componentModel))
            {
                attributes.Add("disabled");
            }
            if (Required)
            {
                attributes.Add("required");
            }

            FormMode mode = componentModel is FormModel ? ((FormModel)componentModel).Mode : FormMode.Update;
            bool readOnly = componentModel is FormModel ? ((FormModel)componentModel).ReadOnly : false;

            if ((IsReadOnly(mode) && LookupOptions == null) || readOnly)
            {
                attributes.Add("readonly");
            }

            if (attr != null)
            {
                attributes.AddRange(attr);
            }

            return string.Join(" ", attributes);
        }
        string CheckboxAttributes(ComponentModel componentModel, bool boolValue)
        {
            List<string> attributes = new List<string>();
            FormMode mode = componentModel is FormModel ? ((FormModel)componentModel).Mode : FormMode.Update;
            if (Disabled || mode == FormMode.Empty)
            {
                attributes.Add("disabled");
            }

            if (boolValue)
            {
                attributes.Add("checked");
            }

            return string.Join(" ", attributes);
        }

        bool Disable(ComponentModel componentModel)
        {
            FormMode mode = componentModel is FormModel ? ((FormModel)componentModel).Mode : FormMode.Update;
            if (mode == FormMode.Insert && PrimaryKeyRequired)
            {
                return false;
            }
            return (PrimaryKey || ForeignKey || Disabled || mode == FormMode.Empty);
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
                    ControlType = FormControlType.Number;
                    attributes["type"] = nameof(FormControlType.Number).ToLower();
                    attributes[attrName] = attrValue?.ToString() ?? String.Empty;
                    break;
                case nameof(DateTime):
                    ControlType = FormControlType.Date;
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