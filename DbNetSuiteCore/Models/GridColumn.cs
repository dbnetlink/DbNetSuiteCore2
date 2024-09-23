using DbNetSuiteCore.Enums;
using System.Data;

namespace DbNetSuiteCore.Models
{
    public class GridColumn : ColumnModel
    {
        public bool Searchable => (DataType == typeof(string) && DbDataType != nameof(System.Data.SqlTypes.SqlXml));
        public bool Sortable => DbDataType != nameof(System.Data.SqlTypes.SqlXml);
        public bool Editable { get; set; } = false;
        public bool Viewable { get; set; } = true;
        public bool PrimaryKey { get; set; } = false;
        public bool ForeignKey { get; set; } = false;
        public int? MaxTextLength { get; set; }
        public AggregateType Aggregate { get; set; } = AggregateType.None;

        public SortOrder? InitialSortOrder { get; set; } = null;
        public bool Filter { get; set; } = true;
        public string Style { get; set; } = string.Empty;
        public bool DataOnly { get; set; } = false;

        public GridColumn()
        {
        }
        public GridColumn(string expression, string label) : base(expression, label)
        {
        }

        public GridColumn(DataColumn dataColumn) : base(dataColumn)
        {
        }
        
        public GridColumn(DataRow dataRow, DataSourceType dataSourceType) : base(dataRow)
        {
        }

        public GridColumn(string name) : base(name, name)
        {
        }
    }
}
