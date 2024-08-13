using DbNetTimeCore.Enums;
using System.Data;

namespace TQ.Models
{
    public class GridColumnModel : ColumnModel
    {
        public bool Searchable => (DataType == typeof(string) && DbDataType != nameof(System.Data.SqlTypes.SqlXml));
        public bool Sortable => DbDataType != nameof(System.Data.SqlTypes.SqlXml);
        public bool Editable { get; set; } = false;
        public int? MaxTextLength { get; set; }
        public int Ordinal { get; set; }
        public SortOrder? InitialSortOrder { get; set; }
        public bool Filter { get; set; } = false;
        public string ParamName => $"Param{Ordinal}";
        public GridColumnModel()
        {
        }
        public GridColumnModel(string expression, string label) : base(expression, label)
        {
        }

        public GridColumnModel(DataColumn dataColumn) : base(dataColumn)
        {
        }
        
        public GridColumnModel(DataRow dataRow, DataSourceType dataSourceType) : base(dataRow, dataSourceType)
        {
        }

        public GridColumnModel(string name) : base(name, name)
        {
        }
    }
}
