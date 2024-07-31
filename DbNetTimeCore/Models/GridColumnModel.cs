using DbNetTimeCore.Helpers;
using System.Data;
using System.Text.RegularExpressions;

namespace DbNetTimeCore.Models
{
    public class GridColumnModel : ColumnModel
    {
        public bool Searchable { get; set; } = false;
        public bool Editable { get; set; } = false;
        public int? MaxTextLength { get; set; }
        public int Ordinal { get; set; }
        public string ParamName => $"Param{Ordinal}";
        public GridColumnModel()
        {
        }
        public GridColumnModel(string expression, string label, bool searchable = false) : base(expression, label)
        {
            Searchable = searchable;
        }

        public GridColumnModel(DataColumn dataColumn) : base(dataColumn)
        {
            Searchable = dataColumn.DataType == typeof(string);
            Initialised = true;
        }

        public GridColumnModel(string name) : base(name, name)
        {
        }

        public void Update(DataColumn dataColumn)
        {
            DataType = dataColumn.DataType;
            Searchable = dataColumn.DataType == typeof(string);
            Initialised = true;
            Name = dataColumn.ColumnName;
            if (string.IsNullOrEmpty(Label))
            {
                Label = TextHelper.GenerateLabel(Name);
            }
        }
    }
}
