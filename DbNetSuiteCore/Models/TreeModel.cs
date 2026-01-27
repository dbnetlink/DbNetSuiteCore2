using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Extensions;
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
       // internal List<DataTable> DataTables { get; set; } = new List<DataTable>();

        /// <summary>
        /// Placeholder text for the search input
        /// </summary>
        public string InputPlaceholder { get; set; } = "Search...";
        /// <summary>
        /// Placeholder text for selected item
        /// </summary>
        public string SelectionPlaceholder { get; set; } = "Select...";
        /// <summary>
        /// Selection title text
        /// </summary>
        public string SelectionTitle { get; set; } = "";
        /// <summary>
        /// Loads the tree fully expanded (default false)
        /// </summary>
        public bool Expand { get; set; } = false;
        /// <summary>
        /// Allows the tree leaves to be selectable (default true)
        /// </summary>
        public bool LeafSelectable { get; set; } = true;
        /// <summary>
        /// Makes the tree items available as a drop down pane
        /// </summary>
        public bool DropDown { get; set; } = true;
        /// <summary>
        /// Allows the tree nodes to be selectable  (default false)
        /// </summary>
        public bool NodeSelectable { get; set; } = false;
        /// <summary>
        /// Enables simple search functionality 
        /// </summary>
        public bool Search { get; set; } = false;
        /// <summary>
        /// Select distinct values only (database only) 
        /// </summary>
        public bool Distinct { get; set; } = false;
        /// <summary>
        /// Defines the max-height CSS style value for the tree items pane
        /// </summary>
        public string MaxHeight { get; set; } = "30rem";
        /// <summary>
        /// Defines the min-width CSS style value for the tree items pane
        /// </summary>
        public string MinWidth { get; set; } = "20rem";

        public TreeModel() : base()
        {
        }
        public TreeModel(DataSourceType dataSourceType, string url) : base(dataSourceType, url)
        {
        }

        public TreeModel(Type dataSourcePlugin, Type dataSourcePluginType) : base(dataSourcePlugin, dataSourcePluginType)
        {
        }

        public TreeModel(DataSourceType dataSourceType, string connectionAlias, string tableName) : base(dataSourceType, connectionAlias, tableName)
        {
        }
 
        public TreeModel(string tableName) : base(tableName)
        {
        }

        internal string ForeignKeyName => Columns.FirstOrDefault(c => c.ForeignKey)?.Expression ?? string.Empty;

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
                return dataRow[dataColumn];
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
                int index = (Columns.Where(c => c.ForeignKey == false).Count() == 1) ? 0 : 1;
                return dataRow[index].ToString();
            }
        }


        internal string PathValue(DataRow dataRow)
        {
            if (DataSourceType == DataSourceType.FileSystem)
            {
                return dataRow.RowValue(FileSystemColumn.Path).ToString();
            }
            return string.Empty;
        }

        [JsonIgnore]
        public List<TreeModel> Levels => GetLevels();

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

        internal void ClearNestedLevels()
        {
            _nestedLevels.Clear();
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