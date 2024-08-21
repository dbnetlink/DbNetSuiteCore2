using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Repositories;
using System.Data;

namespace DbNetSuiteCore.Models
{
    public class EditColumnModel : ColumnModel
    {
        private EditControlType? _editControlType = null;
        public string ClassName { get; set; } = "w-full";
        public string ErrorClassName => $"{UIControlPrefix()}-error in-error";
        public QueryCommandConfig? Lookup { get; set; }
        public Type? LookupEnum { get; set; }
        public DataTable LookupValues { get; set; } = new DataTable();
        public EditControlType EditControlType {
            get 
            {
                if (_editControlType != null)
                {
                    return _editControlType.Value;
                }
                else if (Lookup != null)
                {
                    return EditControlType.Lookup;
                }
                else if (LookupEnum != null)
                {
                    return EditControlType.EnumLookup;
                }
                else if (DataType == typeof(Boolean))
                {
                    return EditControlType.Checkbox;
                }
                return EditControlType.Input;
            }
            set 
            { 
                _editControlType = value; 
            } 
        }
        public bool Required { get; set; } = false;
        public bool Invalid { get; set; } = false;

        public EditColumnModel()
        {
        }
        public EditColumnModel(string name, string label) : base(name, label)
        {
            Name = name;
            Label = label;
        }

        public string ClassNames
        {
            get
            {
                List<string> classNamesList = new List<string>();

                switch (EditControlType)
                {
                    case EditControlType.MultiSelect:
                    case EditControlType.Lookup:
                    case EditControlType.EnumLookup:
                        classNamesList.Add("select select-bordered w-full max-w-xs");
                        break;
                    case EditControlType.TextArea:
                        classNamesList.Add("textarea textarea-bordered");
                        break;
                    case EditControlType.Checkbox:
                        classNamesList.Add("checkbox checkbox-lg");
                        break;
                    default:
                        classNamesList.Add("input input-bordered max-w-xs");
                        break;
                }

                if (!string.IsNullOrEmpty(ClassName))
                {
                    classNamesList.Add(ClassName);
                }

                if (Invalid)
                {
                    classNamesList.Add(ErrorClassName);
                }

                return string.Join(" ", classNamesList);
            }
        }

        private string UIControlPrefix()
        {
            switch (EditControlType)
            {
                case EditControlType.MultiSelect:
                case EditControlType.Lookup:
                case EditControlType.EnumLookup:
                    return "select";
                case EditControlType.TextArea:
                    return "textarea";
                case EditControlType.Checkbox:
                    return "checkbox";
                default:
                    return "input";
            }
        }
    }
}