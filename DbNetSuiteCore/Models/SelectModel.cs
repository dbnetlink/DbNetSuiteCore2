using DbNetSuiteCore.Enums;
using System.Data;
using System.Text.Json.Serialization;

namespace DbNetSuiteCore.Models
{
    public class SelectModel : ComponentModel
    {
        private SortOrder? _SortSequence = SortOrder.Asc;
        public List<string> LinkedSelectIds => GetLinkedControlIds(nameof(SelectModel));
        public IEnumerable<SelectColumn> Columns { get; set; } = new List<SelectColumn>();

        [JsonIgnore]
        public IEnumerable<SelectColumn> NonOptionGroupColumns => Columns.Where(c => c.OptionGroup == false);
        [JsonIgnore]
        public SelectColumn ValueColumn => NonOptionGroupColumns.First();
        [JsonIgnore]
        public SelectColumn DescriptionColumn => NonOptionGroupColumns.Count() == 1 ? NonOptionGroupColumns.First() : NonOptionGroupColumns.Skip(1).First();
        [JsonIgnore]
        public SelectColumn OptionGroupColumn => Columns.Where(c => c.OptionGroup).First();
        [JsonIgnore]
        public IEnumerable<SelectColumn> SearchableColumns
        {
            get
            {
                var searchableColumns = new List<SelectColumn>() { DescriptionColumn };
                if (IsGrouped)
                {
                    searchableColumns.Add(Columns.First(c => c.OptionGroup));
                }
                return searchableColumns;
            }
        }
        public Dictionary<SelectClientEvent, string> ClientEvents { get; set; } = new Dictionary<SelectClientEvent, string>();
        public int Size { get; set; } = 1;
        public string EmptyOption { get; set; } = string.Empty;
        public bool Searchable { get; set; } = false;
        internal override SelectColumn? SortColumn => DescriptionColumn;
        internal override SortOrder? SortSequence
        {
            get { return _SortSequence; }
            set { _SortSequence = value; }
        }
        public RowSelection RowSelection
        {
            get { return _RowSelection; }
            set { _RowSelection = value; }
        }

        public bool IsGrouped => Columns.Any(c => c.OptionGroup);

        public SelectModel() : base()
        {
        }
        public SelectModel(DataSourceType dataSourceType, string url) : base(dataSourceType, url)
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

        public override IEnumerable<ColumnModel> GetColumns()
        {
            return Columns.Cast<ColumnModel>();
        }

        public override void SetColumns(IEnumerable<ColumnModel> columns)
        {
            Columns = columns.Cast<SelectColumn>();
        }

        public override ColumnModel NewColumn(DataRow dataRow)
        {
            return new SelectColumn(dataRow);
        }
        public override ColumnModel NewColumn(DataColumn dataColumn, DataSourceType dataSourceType)
        {
            return new SelectColumn(dataColumn, dataSourceType);
        }

        public void Bind(SelectClientEvent clientEvent, string functionName)
        {
            ClientEvents[clientEvent] = functionName;
        }
    }
}