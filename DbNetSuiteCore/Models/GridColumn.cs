using DbNetSuiteCore.Enums;
using System.ComponentModel;
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
        public AggregateType Aggregate { get; set; } = AggregateType.None;
        public SortOrder? InitialSortOrder { get; set; } = null;
        public bool Filter { get; set; } = false;
        public string Style { get; set; } = string.Empty;
        public bool DataOnly { get; set; } = false;
        public Image? Image { get; set; }

        [Description("Apply reglular expression to value before displaying")]
        public string RegularExpression { get; set; } = string.Empty;
        [Description("Apply a format template to the column value e.g. <b>{0}</b>")]

        public GridColumn()
        {
        }
        public GridColumn(string expression, string label) : base(expression, label)
        {
        }

        public GridColumn(DataColumn dataColumn, DataSourceType dataSourceType) : base(dataColumn, dataSourceType)
        {
        }
        
        public GridColumn(DataRow dataRow) : base(dataRow)
        {
        }

        public GridColumn(string name) : base(name, name)
        {
        }
    }
}
