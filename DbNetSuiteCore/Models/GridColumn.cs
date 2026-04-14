using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using MongoDB.Bson;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Data;

namespace DbNetSuiteCore.Models
{
    public class GridColumn : ColumnModel
    {
        private FilterType _Filter = FilterType.None;
        private bool _Editable = false;
        internal bool Sortable => IsSortable();
        internal bool Editable
        {
            get { return FormColumn != null && PrimaryKey == false && ForeignKey == false; }
            set
            {
                _Editable = value;
            }
        }
        /// <summary>
        /// Controls the presecense of a column in the View Dialog. Default is true.
        /// </summary>
        /// <remarks>
        /// If you want a column to only show in the View Dialog set the DataOnly property to true.
        /// </remarks>
        public bool Viewable { get; set; } = true;
        /// <summary>
        /// Causes the value in the column to be aggregated by the non-aggregated columns in the grid
        /// </summary>
        public AggregateType Aggregate { get; set; } = AggregateType.None;
        /// <summary>
        /// Specified the type of column filter rendered in the grid filter row. Can be used in conjunction with Lookup property provide a custom list of values.
        /// </summary>
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
        [JsonProperty]
        internal string FilterError { get; set; } = string.Empty;
        /// <summary>
        /// Applies a specific CSS style to the column cells.
        /// </summary>
        public string Style { get; set; } = string.Empty;
        /// <summary>
        /// Provides configuration information for a column to render and image from binary data.
        /// </summary>
        public Image Image { get; set; }
        /// <summary>
        /// Specifies the maximum number of characters shown in cell beyond which the value will be truncated.
        /// </summary>
        /// <remarks>
        /// Truncated values can be seen in full by hovering over the cell.
        /// </remarks>
        public int MaxChars { get; set; } = int.MaxValue;
        internal bool NoFormat => Editable && (FormColumn?.MaxValue != null || FormColumn?.MinValue != null);
        /// <summary>
        /// Applies a regular expression to the column value before rendering in the cell
        /// </summary>
        /// <remarks>
        /// For example, to wrap the value in a <div> tag. RegularExpression = "<p data-summary>(.*?)</p>"
        /// </remarks>
        public string RegularExpression { get; set; } = string.Empty;
        [JsonIgnore]
        internal List<bool> LineInError { get; set; } = new List<bool>();
        /// <summary>
        /// To make a column editable assign a new instance of a FormColumn and configure as required
        /// </summary>
        /// <remarks>
        /// The constructor for the FormColumn object should be left empty as these properties are inhertited from the GridColumn
        /// </remarks>
        public FormColumn FormColumn { get; set; } = null;
        /// <summary>
        /// Specifies a custom parsing format for string values that should be treated as dates or numbers.
        /// </summary>
        /// <remarks>
        /// Should be used in conjucntion with the DataType property to coerce the column value to the new type. For example,  {DataType = typeof(DateTime), ParseFormat="yyyyMMdd"}
        /// </remarks>
        public string ParseFormat { get; set; } = string.Empty;
        /// <summary>
        /// Controls the visibility of a column in the grid. Default is true.
        /// </summary>
        public bool Visible { get; set; } = true;

        public GridColumn()
        {
        }
        public GridColumn(string name) : base(name, TextHelper.GenerateLabel(name))
        {
        }

        public GridColumn(string expression, string label) : base(expression, label)
        {
        }

        public GridColumn(string expression, string label, string alias) : base(expression, label, alias)
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
                case nameof(MSSQLDataTypes.Xml) when DataSource == DataSourceType.MSSQL:
                case nameof(MSSQLDataTypes.Text) when DataSource == DataSourceType.MSSQL:
                case nameof(PostgreSqlDataTypes.Json) when DataSource == DataSourceType.PostgreSql:
                case nameof(OracleDataTypes.Clob) when DataSource == DataSourceType.Oracle:
                case nameof(OracleDataTypes.Blob) when DataSource == DataSourceType.Oracle:
                case nameof(OracleDataTypes.NClob) when DataSource == DataSourceType.Oracle:
                case nameof(OracleDataTypes.XmlType) when DataSource == DataSourceType.Oracle:
                case nameof(OracleDataTypes.Raw) when DataSource == DataSourceType.Oracle:
                        return false;
            }

            return (DataOnly == false);
        }

        internal static List<KeyValuePair<string, string>> BooleanFilterOptions => new List<KeyValuePair<string, string>>()
        {
            new KeyValuePair<string, string>("1","Yes"),
            new KeyValuePair<string, string>("0","No"),
        };
    }
}