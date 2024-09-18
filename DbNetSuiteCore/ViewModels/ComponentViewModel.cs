using System.Data;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.ViewModels
{
    public class ComponentViewModel
    {
        private ComponentModel _componentModel;
        public IEnumerable<DataColumn> DataColumns => _componentModel.Data.Columns.Cast<DataColumn>();

        public string SubmitUrl => $"/gridcontrol.htmx";

        public ComponentViewModel(ComponentModel componentModel)
        {
            _componentModel = componentModel;
        }

        protected ColumnModel? _GetColumnInfo(DataColumn column, IEnumerable<ColumnModel> columns)
        {
            return columns.FirstOrDefault(c => c.Name == column.ColumnName || c.Name.Split(".").Last() == column.ColumnName);
        }
    }
}
