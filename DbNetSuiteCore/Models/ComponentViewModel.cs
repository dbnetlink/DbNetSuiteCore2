using System.Data;
namespace DbNetSuiteCore.Models
{
    public class ComponentViewModel
    {
        private ComponentModel _componentModel;
        public IEnumerable<DataColumn> Columns => _componentModel.Data.Columns.Cast<DataColumn>();

        public string SubmitUrl => $"/gridcontrol.htmx";
      
        public List<GridColumnModel> ColumnInfo => _componentModel.Columns;
        public ComponentViewModel(ComponentModel componentModel)
        {
            _componentModel = componentModel;

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

            /*
            if (columnInfo == null)
            {
                throw new Exception(column.ColumnName); 
            }
            */

            return columnInfo;
        }
    }
}
