using DbNetSuiteCore.Enums;
using DocumentFormat.OpenXml.Drawing.Charts;
using MongoDB.Bson;
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
        public SelectColumn? ValueColumn => NonOptionGroupColumns.FirstOrDefault();
        [JsonIgnore]
        public SelectColumn? DescriptionColumn => Columns.Any() ? (NonOptionGroupColumns.Count() == 1 ? NonOptionGroupColumns.First() : NonOptionGroupColumns.Skip(1).First()) : null;
        [JsonIgnore]
        public SelectColumn? OptionGroupColumn => Columns.Where(c => c.OptionGroup).FirstOrDefault();
        [JsonIgnore]
        public override IEnumerable<SelectColumn> SearchableColumns
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
        public LayoutType Layout { get; set; } = LayoutType.Column;

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

        public override ColumnModel NewColumn(DataRow dataRow, DataSourceType dataSourceType)
        {
            return new SelectColumn(dataRow, dataSourceType);
        }
        public override ColumnModel NewColumn(DataColumn dataColumn, DataSourceType dataSourceType)
        {
            return new SelectColumn(dataColumn, dataSourceType);
        }

        public override ColumnModel NewColumn(BsonElement element)
        {
            return new SelectColumn(element);
        }

        public void Bind(SelectClientEvent clientEvent, string functionName)
        {
            ClientEvents[clientEvent] = functionName;
        }
    }
}