using DbNetTimeCore.Enums;
using System.Data;
using static DbNetTimeCore.Utilities.DbNetDataCore;

namespace DbNetTimeCore.Models
{
    public class EditColumnModel : ColumnModel
    {
        public string ClassName { get; set; } = "w-full";
        public string ErrorClassName => $"{UIControlPrefix}-error";
        public QueryCommandConfig? Lookup { get; set; }
        public Type? LookupEnum { get; set; }
        public DataTable LookupValues { get; set; } = new DataTable();
        public EditControlType? EditControlType { get; set; }
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
                if (EditControlType != null)
                {
                    switch (EditControlType.Value)
                    {
                        case Enums.EditControlType.MultiSelect:
                            classNamesList.Add("select select-bordered w-full max-w-xs");
                            break;
                        case Enums.EditControlType.TextArea:
                            classNamesList.Add("textarea textarea-bordered");
                            break;
                    }
                }
                else if (Lookup != null)
                {
                    classNamesList.Add("select select-bordered w-full max-w-xs");
                }
                else if (LookupEnum != null)
                {
                    classNamesList.Add("select select-bordered w-full max-w-xs");
                }
                else if (DataType == typeof(Boolean))
                {
                    classNamesList.Add("checkbox checkbox-lg");
                }
                else
                {
                    classNamesList.Add("input input-bordered max-w-xs");
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
            if (EditControlType != null)
            {
                switch (EditControlType.Value)
                {
                    case Enums.EditControlType.MultiSelect:
                        return "select";
                    case Enums.EditControlType.TextArea:
                        return "textarea";
                }
            }
            else if (Lookup != null)
            {
                return "select";
            }
            else if (LookupEnum != null)
            {
                return "select";
            }
            else if (DataType == typeof(Boolean))
            {
                return "checkbox";
            }
            else
            {
                return "input";
            }

            return string.Empty;
        }
    }
}
