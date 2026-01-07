using DbNetSuiteCore.Enums;
using MongoDB.Bson;
using Newtonsoft.Json;
using System.Data;
namespace DbNetSuiteCore.Models
{
    public class TreeModel : GridSelectModel
    {
        private SortOrder? _SortSequence = SortOrder.Asc;
        [JsonProperty]
        internal List<string> LinkedSelectIds => GetLinkedControlIds(nameof(TreeModel));
        public IEnumerable<TreeColumn> Columns { get; set; } = new List<TreeColumn>();

        [JsonIgnore]
        internal IEnumerable<TreeColumn> NonOptionGroupColumns => Columns.Where(c => c.OptionGroup == false);
        [JsonIgnore]
        internal TreeColumn ValueColumn => NonOptionGroupColumns.FirstOrDefault() ?? new TreeColumn();
        [JsonIgnore]
        internal TreeColumn DescriptionColumn => Columns.Any() ? (NonOptionGroupColumns.Count() == 1 ? NonOptionGroupColumns.First() : NonOptionGroupColumns.Skip(1).First()) : new TreeColumn();
        [JsonIgnore]
        internal override IEnumerable<TreeColumn> SearchableColumns
        {
            get
            {
                var searchableColumns = new List<TreeColumn>();

                if (DescriptionColumn != null)
                {
                    searchableColumns.Add(DescriptionColumn);
                }
                if (IsGrouped)
                {
                    searchableColumns.Add(Columns.First(c => c.OptionGroup));
                }
                return searchableColumns;
            }
        }
        /// <summary>
        /// Use this property or the Bind method to assign the name of a client-side JavaScript function to be executed for the specified client event.
        /// </summary>
        public Dictionary<TreeClientEvent, string> ClientEvents { get; set; } = new Dictionary<TreeClientEvent, string>();
        /// <summary>
        /// Sets the number of visible rows in the Select. Default is 1 (drop-down)
        /// </summary>
        public int Size { get; set; } = 1;
        /// <summary>
        /// Specifies the text for the empty option 
        /// </summary>
        public string EmptyOption { get; set; } = string.Empty;
        /// <summary>
        /// When set to true the control renders a separate input box that can be used to filter the select options
        /// </summary>
        public bool Searchable { get; set; } = false;
        /// <summary>
        /// When set to true will only return distinct values of the selected columns
        /// </summary>
        public bool Distinct { get; set; } = false;
        internal override TreeColumn SortColumn => DescriptionColumn;
        internal override SortOrder? SortSequence
        {
            get { return _SortSequence; }
            set { _SortSequence = value; }
        }
        /// <summary>
        /// Defines whether single or multiple options can be selected. When set to Multiple it is suggested that the Size property is also set to a value > 1.
        /// </summary>
        public RowSelection RowSelection
        {
            get { return _RowSelection; }
            set { _RowSelection = value; }
        }

        internal bool IsGrouped => Columns.Any(c => c.OptionGroup);
        /// <summary>
        /// Controls the layout of the Caption, Search box and Select element. Column (default) renders one above the other and Row renders them across the page
        /// </summary>
        public LayoutType Layout { get; set; } = LayoutType.Column;

        public TreeModel() : base()
        {
        }
        public TreeModel(DataSourceType dataSourceType, string url) : base(dataSourceType, url)
        {
        }

        public TreeModel(DataSourceType dataSourceType, string connectionAlias, string tableName, bool isStoredProcedure = false) : base(dataSourceType, connectionAlias, tableName, isStoredProcedure)
        {
        }

        public TreeModel(DataSourceType dataSourceType, string connectionAlias, string procedureName, List<DbParameter> procedureParameters) : base(dataSourceType, connectionAlias, procedureName, procedureParameters)
        {
        }

        public TreeModel(string tableName) : base(tableName)
        {
        }

        internal override IEnumerable<ColumnModel> GetColumns()
        {
            return Columns.Cast<ColumnModel>();
        }

        internal override void SetColumns(IEnumerable<ColumnModel> columns)
        {
            Columns = columns.Cast<TreeColumn>();
        }

        internal override ColumnModel NewColumn(DataRow dataRow, DataSourceType dataSourceType)
        {
            return new TreeColumn(dataRow, dataSourceType);
        }
        internal override ColumnModel NewColumn(DataColumn dataColumn, DataSourceType dataSourceType)
        {
            return new TreeColumn(dataColumn, dataSourceType);
        }

        internal override ColumnModel NewColumn(BsonElement element)
        {
            return new TreeColumn(element);
        }

        public void Bind(TreeClientEvent clientEvent, string functionName)
        {
            ClientEvents[clientEvent] = functionName;
        }
    }
}