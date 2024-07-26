using DbNetTimeCore.Helpers;

namespace DbNetTimeCore.Models
{
    public class FormModel : ComponentModel
    {
        public List<EditColumnModel> EditColumns => Columns.Cast<EditColumnModel>().ToList();
        public string? PrimaryKey { get; set; }
        public int ColSpan { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Error { get; set; } = false;
        public Dictionary<string, object> SavedFormValues { get; set; } = new Dictionary<string, object>();

        public string Id { get; set; }
       
        public Dictionary<string, object> FormValues(FormCollection form)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            foreach (var column in Columns.Where(c => c.IsPrimaryKey == false))
            {
                switch (column.DataType.Name)
                {
                    case "Boolean":
                        parameters[$"{column.Name}"] = RequestHelper.FormValue(column.Name, "", form) == "on" ? "1" : "0";
                        break;
                    default:
                        parameters[$"{column.Name}"] = RequestHelper.FormValue(column.Name, "", form);
                        break;
                }
            }

            return parameters;
        }
    }
}