using System.Data;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.ViewModels
{
    public class ComponentViewModel
    {
        private ComponentModel _componentModel;
        public IEnumerable<DataColumn> DataColumns => _componentModel.Data.Columns.Cast<DataColumn>();

        public string SubmitUrl => _componentModel.PostUrl;
        public string Diagnostics { get; set; } = string.Empty;

        public ComponentViewModel(ComponentModel componentModel)
        {
            _componentModel = componentModel;
        }

        protected ColumnModel? _GetColumnInfo(DataColumn column, IEnumerable<ColumnModel> columns)
        {
            return columns.FirstOrDefault(c => c.Name == column.ColumnName || c.Name.Split(".").Last() == column.ColumnName);
        }

        public DataColumn? GetDataColumn(ColumnModel column)
        {
            return _componentModel.GetDataColumn(column);
        }

        public bool IsFolder(DataRow dataRow)
        {
            return Convert.ToBoolean(_componentModel.RowValue(dataRow, FileSystemColumn.IsDirectory.ToString(), false));
        }

        public string LinkedControlIds => string.Join(",", _componentModel.GetLinkedControlIds());

    }
}
