using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using MongoDB.Bson;
using System.ComponentModel;
using System.Data;
using System.Text.Json.Serialization;

namespace DbNetSuiteCore.Models
{
    public class GridColumn : GridFormColumn
    {
        private FilterType _Filter = FilterType.None;
        private bool _Editable = false;
        public bool Sortable => IsSortable();
        public bool Editable
        {
            get { return _Editable && PrimaryKey == false && ForeignKey == false; }
            set
            {
                _Editable = value;
            }
        }
        public bool Edit
        {
            set
            {
                _Editable = value;
            }
        }

        public bool Viewable { get; set; } = true;
        public AggregateType Aggregate { get; set; } = AggregateType.None;
        public FilterType Filter
        {
            get { return _Filter; }
            set
            {
                _Filter = value;
                if (value == FilterType.Distinct && Lookup == null)
                {
                    Lookup = new Lookup();
                }
            }
        }
        public string FilterError { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;
        public Image? Image { get; set; }
        public int MaxChars { get; set; } = int.MaxValue;
        internal bool NoFormat => Editable && (MaxValue != null || MinValue != null);
        [Description("Apply reglular expression to value before displaying")]
        public string RegularExpression { get; set; } = string.Empty;
        [JsonIgnore]
        public List<bool> LineInError { get; set; } = new List<bool>();
             
        public FormColumn FormColumn { get; set; } = new FormColumn();
     

        public GridColumn()
        {
        }
        public GridColumn(string name) : base(name, TextHelper.GenerateLabel(name))
        {
        }

        public GridColumn(string expression, string label) : base(expression, label)
        {
        }

        internal GridColumn(DataColumn dataColumn, DataSourceType dataSourceType) : base(dataColumn, dataSourceType)
        {
        }

        internal GridColumn(DataRow dataRow, DataSourceType dataSourceType) : base(dataRow, dataSourceType)
        {
        }
        internal GridColumn(BsonElement element) : base(element)
        {
        }

        private bool IsSortable()
        {
            switch (DbDataType)
            {
                case nameof(MSSQLDataTypes.Xml):
                case nameof(MSSQLDataTypes.Text):
            //    case nameof(MSSQLDataTypes.Ntext):
                case nameof(PostgreSqlDataTypes.Json):
                case nameof(OracleDataTypes.Clob):
                case nameof(OracleDataTypes.Blob):
                case nameof(OracleDataTypes.NClob):
                case nameof(OracleDataTypes.XmlType):
                case nameof(OracleDataTypes.Raw):
                        return false;
            }

            return (DataOnly == false);
        }
    }
}