using System.Data;
namespace TQ.Models
{
    public class FormViewModel : ComponentViewModel
    {
        private readonly FormModel _formModel = new FormModel();
        public DataRow Row { get; set; }

        public string Id { get; set; }

        public string FormId => $"{Id}Form";

        public int ColSpan => _formModel.ColSpan;
      
        public string Message => _formModel.Message;
        public bool InErrorState => _formModel.EditColumns.Any(c => c.Invalid) || _formModel.Error;
        public string SaveUrl(DataRow row)
        {
            return $"/{Id}/?handler=save&pk={PrimaryKeyValue(row)}";
        }

        public FormViewModel(DataTable dataTable, string id, FormModel formModel) : base(dataTable, formModel)
        {
            _formModel = formModel;
            if (dataTable.Rows.Count != 1)
            {
                throw new Exception("DataTable for form view model should contain 1 and only 1 row");
            }
            Row = dataTable.Rows[0];
        }

        public EditColumnModel? GetColumnInfo(DataColumn column)
        {
            return (EditColumnModel)_GetColumnInfo(column);
        }


        public string ColumnValue(DataColumn column)
        {
            return InErrorState ? SavedFormValue(column.ColumnName) : Row[column]?.ToString() ?? string.Empty;
        }

        private string SavedFormValue(string columnName)
        {
            if (_formModel.SavedFormValues.ContainsKey(columnName))
            {
                return _formModel.SavedFormValues[columnName]?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }
    }
}
