using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Extensions;
using DocumentFormat.OpenXml.Bibliography;
using MongoDB.Bson;
using Newtonsoft.Json;
using System.Data;
namespace DbNetSuiteCore.Models
{
    public class TreeModel : ComponentModel
    {
        [JsonProperty]
        internal List<TreeModel> _nestedLevels = new List<TreeModel>();
        private SortOrder? _SortSequence = SortOrder.Asc;
        [JsonProperty]
        public IEnumerable<TreeColumn> Columns { get; set; } = new List<TreeColumn>();
        internal TreeModel ParentLevel = null;
        [JsonIgnore]
        internal override IEnumerable<TreeColumn> SearchableColumns
        {
            get
            {
                return new List<TreeColumn>();
            }
        }

        public Dictionary<TreeClientEvent, string> ClientEvents { get; set; } = new Dictionary<TreeClientEvent, string>();
        internal override TreeColumn SortColumn => (Columns.Count() == 1) ? Columns.First() : Columns.Skip(1).First();
        internal override SortOrder? SortSequence
        {
            get { return SortOrder.Asc; }
            set { _SortSequence = value; }
        }

        internal List<DataTable> DataTables { get; set; } = new List<DataTable>();

        public string InputPlaceholder { get; set; } = "Search...";
        public string SelectionPlaceholder { get; set; } = "Select...";
        public string SelectionTitle { get; set; } = "";
        public bool Expand { get; set; } = false;
        /// <summary>
        /// Enables simple search functionality 
        /// </summary>
        public bool Search { get; set; } = false;

        public TreeModel() : base()
        {
        }
        public TreeModel(DataSourceType dataSourceType, string url) : base(dataSourceType, url)
        {
        }

        public TreeModel(DataSourceType dataSourceType, string connectionAlias, string tableName) : base(dataSourceType, connectionAlias, tableName)
        {
        }
 
        public TreeModel(string tableName) : base(tableName)
        {
        }

        internal string ForeignKeyName => Columns.FirstOrDefault(c => c.ForeignKey).Expression ?? string.Empty;

        internal ColumnModel ForeignKeyColumn => Columns.FirstOrDefault(c => c.ForeignKey);

        internal object PrimaryKeyValue(DataRow dataRow)
        {
            if (DataSourceType == DataSourceType.FileSystem)
            {
                return dataRow.RowValue(FileSystemColumn.Name);
            }
            else
            {
                var column = Columns.FirstOrDefault(c => c.PrimaryKey) ?? Columns.First();
                DataColumn dataColumn = GetDataColumn(column);
                return DbNetSuiteCore.Extensions.DataTableExtensions.QuotedValue(column, dataRow[dataColumn]);
            }
        }

        internal string Description(DataRow dataRow)
        {
            if (DataSourceType == DataSourceType.FileSystem)
            {
                return dataRow.RowValue(FileSystemColumn.Name).ToString();
            }
            else
            {
                int index = (Columns.Count() == 1) ? 0 : 1;
                return dataRow[index].ToString();
            }
        }

        internal List<TreeModel> Levels => GetLevels();

        private List<TreeModel> GetLevels()
        {
            List<TreeModel> levels = new List<TreeModel>() { this };
            foreach (TreeModel nestedLevel in _nestedLevels)
            {
                levels.AddRange(nestedLevel.GetLevels());
            }
            return levels;
        }

        public TreeModel NestedLevel
        {
            set
            {
                AddNestedLevel(value);
            }
        }
        private void AddNestedLevel(TreeModel treeModel)
        {
           AssignLinkedProperties(this, treeModel);
            foreach (TreeModel nestedLevel in treeModel._nestedLevels)
            {
                AssignLinkedProperties(treeModel, nestedLevel);
            }
            _nestedLevels.Add(treeModel);
        }

        internal override IEnumerable<TreeColumn> GetColumns()
        {
            return Columns.Cast<TreeColumn>();
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