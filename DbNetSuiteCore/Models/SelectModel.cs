using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Plugins.Interfaces;
using MongoDB.Bson;
using Newtonsoft.Json;
using System.Data;
namespace DbNetSuiteCore.Models
{
    public class SelectModel : GridSelectModel
    {
        private SortOrder? _SortSequence = SortOrder.Asc;
        [JsonProperty]
        internal List<string> LinkedSelectIds => GetLinkedControlIds(nameof(SelectModel));
        public IEnumerable<SelectColumn> Columns { get; set; } = new List<SelectColumn>();

        [JsonIgnore]
        internal IEnumerable<SelectColumn> NonOptionGroupColumns => Columns.Where(c => c.OptionGroup == false);
        [JsonIgnore]
        internal SelectColumn ValueColumn => NonOptionGroupColumns.FirstOrDefault() ?? new SelectColumn();
        [JsonIgnore]
        internal SelectColumn DescriptionColumn => Columns.Any() ? (NonOptionGroupColumns.Count() == 1 ? NonOptionGroupColumns.First() : NonOptionGroupColumns.Skip(1).First()) : new SelectColumn();
        [JsonIgnore]
        internal SelectColumn OptionGroupColumn => Columns.Where(c => c.OptionGroup).FirstOrDefault() ?? new SelectColumn();
        [JsonIgnore]
        internal override IEnumerable<SelectColumn> SearchableColumns
        {
            get
            {
                var searchableColumns = new List<SelectColumn>();

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
        public Dictionary<SelectClientEvent, string> ClientEvents { get; set; } = new Dictionary<SelectClientEvent, string>();
        /// <summary>
        /// Sets the number of visible rows in the Select. Default is 1 (drop-down)
        /// </summary>
        public int Size { get; set; } = 1;
        /// <summary>
        /// Specifies the text for the empty option 
        /// </summary>
        public string EmptyOption { get; set; } = string.Empty;
        /// <summary>
        /// When set to true will only return distinct values of the selected columns
        /// </summary>
        public bool Distinct { get; set; } = false;
        internal override SelectColumn SortColumn => DescriptionColumn;
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
        /// <summary>
        /// Enables simple search functionality 
        /// </summary>
        public bool Search { get; set; } = false;

        public SelectModel() : base()
        {
        }
        public SelectModel(DataSourceType dataSourceType, string url) : base(dataSourceType, url)
        {
        }

        public SelectModel(DataSourceType dataSourceType, Type dataSourcePlugin) : base(dataSourceType, dataSourcePlugin)
        {
        }

        public SelectModel(DataSourceType dataSourceType, string connectionAlias, string tableName, bool isStoredProcedure = false) : base(dataSourceType, connectionAlias, tableName, isStoredProcedure)
        {
        }

        public SelectModel(DataSourceType dataSourceType, string connectionAlias, string procedureName, List<DbParameter> procedureParameters) : base(dataSourceType, connectionAlias, procedureName, procedureParameters)
        {
        }

        public SelectModel(string tableName) : base(tableName)
        {
        }

        internal override IEnumerable<ColumnModel> GetColumns()
        {
            return Columns.Cast<ColumnModel>();
        }

        internal override void SetColumns(IEnumerable<ColumnModel> columns)
        {
            Columns = columns.Cast<SelectColumn>();
        }

        internal override ColumnModel NewColumn(DataRow dataRow, DataSourceType dataSourceType)
        {
            return new SelectColumn(dataRow, dataSourceType);
        }
        internal override ColumnModel NewColumn(DataColumn dataColumn, DataSourceType dataSourceType)
        {
            return new SelectColumn(dataColumn, dataSourceType);
        }

        internal override ColumnModel NewColumn(BsonElement element)
        {
            return new SelectColumn(element);
        }
        /// <summary>
        /// Use this method or the ClientEvents property to assign the name of a client-side JavaScript function to be executed for the specified client event.
        /// </summary>
        /// <param name="clientEvent">Type of client-side event</param>
        /// <param name="functionName">Name of the JavaScript function</param>
        public void Bind(SelectClientEvent clientEvent, string functionName)
        {
            ClientEvents[clientEvent] = functionName;
        }
    }
}