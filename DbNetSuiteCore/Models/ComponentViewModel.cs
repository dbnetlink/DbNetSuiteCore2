using System.Data;
namespace TQ.Models
{
    public class ComponentViewModel
    {
        public IEnumerable<DataColumn> Columns { get; set; } = new List<DataColumn>();
        private ComponentModel _componentModel;

        public string SubmitUrl => $"/gridcontrol.htmx";
      
        public List<GridColumnModel> ColumnInfo => _componentModel.Columns;
        public ComponentViewModel(DataTable dataTable, ComponentModel componentModel)
        {
            _componentModel = componentModel;
            Columns = dataTable.Columns.Cast<DataColumn>();

            foreach (DataColumn column in Columns)
            {
                ColumnModel? columnInfo = _GetColumnInfo(column);

                if (columnInfo != null)
                {
                    if (columnInfo.DataType == typeof(string))
                    {
                        columnInfo.DataType = column.DataType;
                    }
                }
            }
        }


        protected ColumnModel? _GetColumnInfo(DataColumn column)
        {
            var columnInfo = ColumnInfo.FirstOrDefault(c => c.Name == column.ColumnName || c.Name.Split(".").Last() == column.ColumnName);

            if (columnInfo == null)
            {
                throw new Exception(column.ColumnName); 
            }

            return columnInfo;
        }
    }
}
