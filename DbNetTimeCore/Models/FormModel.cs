using DbNetTimeCore.Helpers;
using System.Collections.Specialized;

namespace DbNetTimeCore.Models
{
    public class FormModel : ComponentModel
    {
        public List<EditColumnModel> EditColumns => Columns.Cast<EditColumnModel>().ToList();
        public string? PrimaryKey { get; set; }
        public int ColSpan { get; set; }
        public string Message { get; set; } = string.Empty;

        public bool InErrorState => EditColumns.Any(c => c.Invalid);
        public ListDictionary FormValues(FormCollection form)
        {
            ListDictionary parameters = new ListDictionary();

            foreach (var column in Columns.Where(c => c.IsPrimaryKey == false))
            {

                switch (column.DataType.Name)
                {
                    case "Boolean":
                        parameters[$"@{column.Name}"] = RequestHelper.FormValue(column.Name, "", form) == "on" ? 1 : 0;
                        break;
                    default:
                        parameters[$"@{column.Name}"] = RequestHelper.FormValue(column.Name, "", form);
                        break;
                }
            }

            return parameters;
        }
    }
}