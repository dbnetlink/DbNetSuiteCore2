
using System.Data;

namespace DbNetSuiteCore.Models
{
    public class SummaryModel
    {
        private int _rowIdx = 0;
        public string ControlType { get; set; } = string.Empty;
        public Dictionary<string, object[]> Data = new Dictionary<string, object[]>();
        public List<ParentColumnModel> Columns = new List<ParentColumnModel>();
        public int RowIdx {
            get { return HasEmptyOption || RowCount == 0 ? _rowIdx - 1 : _rowIdx; }
            set { _rowIdx = value; }
        } 
        public int RowCount { get; set; } = 0;
        public bool HasEmptyOption { get; set; } = false;
        public Dictionary<string,object> ParentRow => RowIdx < 0 ? new Dictionary<string, object>() : Data.Keys.ToDictionary(k => k, k => Data[k][RowIdx],StringComparer.CurrentCultureIgnoreCase);
        public string Name => ParentRow.Keys.Contains("name") ? ParentRow["name"]?.ToString() ?? string.Empty : string.Empty;
        public SummaryModel()
        {
        }
        public SummaryModel(ComponentModel componentModel)
        {
            ControlType = componentModel.GetType().Name;
            Columns = componentModel.GetColumns().Select(c => new ParentColumnModel { Name = c.Name, PrimaryKey = c.PrimaryKey }).ToList();

            if (componentModel is GridModel gridModel)
            {
                Data = gridModel.Data.Columns.Cast<DataColumn>().Where(c => c.DataType != typeof(Byte[])).ToDictionary(c => c.ColumnName, c => gridModel.Rows.AsEnumerable().Select(r => r[c]).ToArray());
                RowCount = gridModel.Rows.Count();
            }

            if (componentModel is FormModel formModel)
            {
                Data = formModel.Data.Columns.Cast<DataColumn>().Where(c => c.DataType != typeof(Byte[])).ToDictionary(c => c.ColumnName, c => formModel.Data.Rows.Cast<DataRow>().AsEnumerable().Select(r => r[c]).ToArray());
                RowCount = formModel.Data.Rows.Cast<DataRow>().Count();
            }

            if (componentModel is SelectModel selectModel)
            {
                Data = selectModel.Data.Columns.Cast<DataColumn>().Where(c => c.DataType != typeof(Byte[])).ToDictionary(c => c.ColumnName, c => selectModel.Data.Rows.Cast<DataRow>().AsEnumerable().Select(r => r[c]).ToArray());
                RowCount = selectModel.Data.Rows.Cast<DataRow>().Count();
                HasEmptyOption = string.IsNullOrEmpty(selectModel.EmptyOption) == false;
            }
        }
    }
}