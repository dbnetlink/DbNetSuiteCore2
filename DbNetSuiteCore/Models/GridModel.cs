using DbNetSuiteCore.Enums;
using System.Data;
using MongoDB.Bson;
using DbNetSuiteCore.Constants;
using DbNetSuiteCore.Helpers;
using Newtonsoft.Json;

namespace DbNetSuiteCore.Models
{
    public class GridModel : GridSelectModel
    {
        private string _SortKey = string.Empty;
        private SortOrder? _SortSequence = null;
		[JsonIgnore]
		internal IEnumerable<GridColumn> VisbleColumns => Columns.Where(c => c.DataOnly == false);
		[JsonIgnore]
		internal IEnumerable<GridColumn> FilterColumns => Columns.Where(c => c.Filter != FilterType.None);
		[JsonIgnore]
		internal IEnumerable<GridColumn> DataOnlyColumns => Columns.Where(c => c.DataOnly);
		[JsonIgnore]
		internal IEnumerable<GridColumn> ContentColumns => Columns.Where(c => c.Expression.StartsWith(FileSystemColumn.Content.ToString()) && string.IsNullOrEmpty(c.RegularExpression) == false);
        [JsonProperty]
        internal int CurrentPage { get; set; } = 1;
         internal string SortKey  
        { 
            get { return string.IsNullOrEmpty(_SortKey) ? InitalSortColumn?.Key ?? string.Empty : _SortKey; } 
            set { _SortKey = value; } 
        }
        [JsonProperty]
        internal string CurrentSortKey { get; set; } = string.Empty;
        [JsonProperty]
        internal SortOrder? CurrentSortSequence { get; set; }
        [JsonProperty]
        internal bool CurrentSortAscending => SortSequence == SortOrder.Asc;
        internal override GridColumn SortColumn => (Columns.FirstOrDefault(c => c.Key == CurrentSortKey) ?? CurrentSortColumn) ?? InitalSortColumn;
        internal GridColumn CurrentSortColumn => Columns.FirstOrDefault(c => c.Key == CurrentSortKey);
        internal GridColumn InitalSortColumn => Columns.FirstOrDefault(c => c.InitialSortOrder.HasValue) ?? Columns.FirstOrDefault(c => c.Sortable);
        internal override SortOrder? SortSequence 
        { 
            get { return _SortSequence == null ? (Columns.FirstOrDefault(c => c.InitialSortOrder.HasValue)?.InitialSortOrder ?? SortOrder.Asc) : _SortSequence; } 
            set { _SortSequence = value; } 
        }
        internal string ExportFormat { get; set; } = string.Empty;
        internal List<string> ColumnFilter { get; set; } = new List<string>();
        /// <summary>
        /// Defines the number of rows rendered on each page with the default number being 20.
        /// </summary>
        public int PageSize { get; set; } = 20;
        [JsonProperty]
        internal bool IsNested { get; set; } = false;
        /// <summary>
        /// When set to true the grid data will also be returned as a JSON dataset that can be accessed using the columnSeriesData and rowSeriesData client-side API methods for integration with 3rd party client-side applications such as charting.
        /// </summary>
        public bool IncludeJsonData { get; set; } = false;
        [JsonIgnore]
        internal string JsonData { get; set; } = string.Empty;
        internal int ColSpan => VisbleColumns.Count();
        internal override IEnumerable<ColumnModel> SearchableColumns => GetColumns().Where(c => c.StringSearchable);
        [JsonProperty]
        internal List<string> LinkedGridIds => GetLinkedControlIds(nameof(GridModel));
        [JsonProperty]
        internal List<GridModel> _NestedGrids { get; set; } = new List<GridModel>();
        internal bool HasNestedGrids => _NestedGrids.Any();
        public IEnumerable<GridColumn> Columns { get; set; } = new List<GridColumn>();
        /// <summary>
        /// Use this property or the Bind method to assign the name of a client-side JavaScript function to be executed for the specified client event.
        /// </summary>
        public Dictionary<GridClientEvent, string> ClientEvents { get; set; } = new Dictionary<GridClientEvent, string>();
        /// <summary>
        /// Set this property to true to improve performance for larger datasets. Only valid for relational database data sources as it uses paging at the database level.
        /// </summary>
        public bool OptimizeForLargeDataset { get; set; } = false;
        internal bool PaginateQuery => OptimizeForLargeDataset && TriggerName != TriggerNames.Download;
        [JsonProperty]
        internal int TotalRows { get; set; }
        internal bool IsGrouped => Columns.Any(c => c.Aggregate != AggregateType.None);
        internal bool IsEditable => Columns.Any(c => c.Editable);
        /// <summary>
        /// Used to access form values when using the CustomisationPlugin property for custom server-side validation.
        /// </summary>
        public Dictionary<string, List<string>> FormValues { get; internal set; } = new Dictionary<string, List<string>>();
        internal string FirstEditableColumnName => Columns.Where(c => c.Editable).First().ColumnName;
        [JsonIgnore]
        internal IEnumerable<DataRow> Rows => OptimizeForLargeDataset? Data.AsEnumerable() : Data.AsEnumerable().Skip((CurrentPage - 1) * PageSize).Take(PageSize);
        [JsonProperty]
        internal List<object> PrimaryKeyValues => Rows.Select(row => PrimaryKeyValue(row) ?? DBNull.Value).ToList();
        [JsonProperty]
        internal List<ModifiedRow> RowsModified { get; set; } = new List<ModifiedRow>();
        [JsonIgnore]
        /// <summary>
        /// Use this property to specify the type of a class that implements the IJsonTransformPlugin interface which at runtime will be instantiated and used to provide runtime transformation of teh selected JSON based data source.
        /// </summary>
        public Type JsonTransformPlugin
        {
            set 
            {
                JsonTransformPluginName = PluginHelper.GetNameFromType(value);
            }
        }

        [JsonProperty]
        internal string JsonTransformPluginName { get; set; } = string.Empty;
        [JsonIgnore]
        /// <summary>
        /// Use this property to specify the type of a class that implements the ICustomGridPlugin interface which at runtime will be instantiated and used to provide runtime initialisation customisation and/or custom server-side validation for editable grids.
        /// </summary>
        public Type CustomisationPlugin
        {
            set{ CustomisationPluginName = PluginHelper.GetNameFromType(value);}
        }
        [JsonProperty]
        internal string CustomisationPluginName { get; set; } = string.Empty;
        /// <summary>
        /// When using JSON as a data source and the array of data is a property then use this to specify the name of the array property.
        /// </summary>
        public string JsonArrayProperty { get; set; } = string.Empty;
        [JsonIgnore]
        /// <summary>
        /// Assign a grid model to this property for it be rendered as a nested child
        /// </summary>
        /// <remarks>
        /// Use HeadingMode.Hidden to hide the heading row or HeadingMode.Frozen to cause the heading row to stay in view when scrolling the grid rows
        /// </remarks>
        public GridModel NestedGrid
        {
            set
            {
                AddNestedGrid(value);
            }
        }

        /// <summary>
        /// Controls the visibility and location of the Toolbar (Hidden, Top or Bottom)
        /// </summary>
        public ToolbarPosition ToolbarPosition { get; set; } = ToolbarPosition.Top;
        /// <summary>
        /// Controls the ability to select none, one or multiple rows. 
        /// </summary>
        public RowSelection RowSelection { 
            get 
            { 
                return _RowSelection;
            } 
            set 
            {   
                _RowSelection = value;
                if (_RowSelection == RowSelection.Multiple)
                {
                    if (MultiRowSelectLocation == MultiRowSelectLocation.None)
                    {
                        MultiRowSelectLocation = MultiRowSelectLocation.Left;
                    }
                }
                else
                {
                    MultiRowSelectLocation = MultiRowSelectLocation.None;
                }
            } 
        }

        /// <summary>
        /// Controls the location of the checkboxes when RowSelection is set to RowSelection.Multiple
        /// </summary>
        public MultiRowSelectLocation MultiRowSelectLocation { get; set; } = MultiRowSelectLocation.None;

        /// <summary>
        /// Controls the visbility and behaviour of the heading row.
        /// </summary>
        /// <remarks>
        /// Use HeadingMode.Hidden to hide the heading row or HeadingMode.Frozen to cause the heading row to stay in view when scrolling the grid rows
        /// </remarks>
        public HeadingMode HeadingMode { get; set; } = HeadingMode.Normal;

        /// <summary>
        /// Model that allows configuration of the View Dialog
        /// </summary>
        /// <remarks>
        /// Allows a user to see additional information about the selected row. By default columns are rendered in the View Dialog. Set the Viewable column property to false to remove from View Dialog. Set the DataOnly property to true for columns that should only render in the View Dialog
        /// </remarks>
        public ViewDialog ViewDialog { get; set; }

        /// <summary>
        /// Specfies the name of the Sheet in the spreadsheet where there are multiple sheets and you want display the one that is not first. DataSourceType.Excel only.
        /// </summary>
        public string SheetName { get; set; } = string.Empty;
        public GridModel() : base()
        {
        }

        /// <summary>
        /// This constructor can be used for a relation databases
        /// </summary>
        /// <param name="DataSourceType dataSourceType">The database type e.g. MSSQL, MySql, PostgreSql, Oracle or SQLite</param>
        /// <param name="string connectionAlias">The name of the connection alias/variable.</param>
        /// <param name="string tableName">The name of the table, view, join statement or stored procedure.</param>
        /// <param name="bool isStoredProcedure = false">Set to true if tableName is the name of a stored procedure</param>
        public GridModel(DataSourceType dataSourceType, string connectionAlias, string tableName, bool isStoredProcedure = false) : base(dataSourceType, connectionAlias, tableName, isStoredProcedure)
        {
        }

        /// <summary>
        /// This constructor can be used for a stored procedure with parameters
        /// </summary>
        /// <param name="DataSourceType dataSourceType">The database type.</param>
        /// <param name="string connectionAlias">The name of the connection alias/variable.</param>
        /// <param name="string procedureName">The name of the stored procedure.</param>
        /// <param name="List<DbParameter>"> procedureParameters">The procedure parameter values.</param>
        public GridModel(DataSourceType dataSourceType, string connectionAlias, string procedureName, List<DbParameter> procedureParameters) : base(dataSourceType, connectionAlias, procedureName, procedureParameters)
        {
        }

        /// <summary>
        /// This constructor can be used for child controls where the dataSourceType and connectionAlias are inhrited from the parent control
        /// </summary>
        /// <param name="string tableName">The name of the table, view or join statement.</param>
        public GridModel(string tableName) : base(tableName)
        {
        }

        internal override IEnumerable<ColumnModel> GetColumns()
        {
            return Columns.Cast<ColumnModel>();
        }

        internal override void SetColumns(IEnumerable<ColumnModel> columns)
        {
            Columns = columns.Cast<GridColumn>();
        }

        internal override ColumnModel NewColumn(DataRow dataRow, DataSourceType dataSourceType)
        { 
            return new GridColumn(dataRow, dataSourceType); 
        }
        internal override ColumnModel NewColumn(DataColumn dataColumn, DataSourceType dataSourceType)
        {
            return new GridColumn(dataColumn, dataSourceType);
        }

        internal override ColumnModel NewColumn(BsonElement element)
        {
            return new GridColumn(element);
        }

        internal void AddNestedGrid(GridModel gridModel)
        {
            _NestedGrids.Add(gridModel);
            gridModel.IsNested = true;

            AssignLinkedProperties(this, gridModel);

            foreach (GridModel nestedGrid in gridModel._NestedGrids)
            {
                AssignLinkedProperties(gridModel, nestedGrid);
            }
        }

        internal void ConfigureSort(string sortKey)
        {
            if (string.IsNullOrEmpty(sortKey) == false)
            {
                if (sortKey != CurrentSortKey)
                {
                    SortSequence = SortOrder.Asc;
                    CurrentSortKey = sortKey;
                }
                else
                {
                    SortSequence = (CurrentSortSequence ?? SortOrder.Asc) == SortOrder.Asc ? SortOrder.Desc : SortOrder.Asc;
                }
                CurrentSortSequence = SortSequence;
            }
            else
            {
                if (string.IsNullOrEmpty(CurrentSortKey))
                {
                    CurrentSortKey = InitalSortColumn?.Key ?? string.Empty;
                    CurrentSortSequence = InitalSortColumn?.InitialSortOrder ?? SortOrder.Asc;
                }
                SortSequence = CurrentSortSequence ?? SortOrder.Asc;
            }
        }

        internal object PrimaryKeyValue(DataRow dataRow)
        {
            if (DataSourceType == DataSourceType.FileSystem)
            {
                return Convert.ToString(RowValue(dataRow, "Name", false));
            }
            else
            {
                List<object> primaryKeyValues = new List<object>();

                foreach (var primaryKeyColumn in Columns.Where(c => c.PrimaryKey))
                {
                    string columnName = primaryKeyColumn.ColumnName;
                    if (primaryKeyColumn.Lookup != null)
                    {
                        columnName = $"{primaryKeyColumn.ColumnName}_value";
                    }
                    var dataColumn = dataRow.Table.Columns.Cast<DataColumn>().ToList().FirstOrDefault(c => c.ColumnName == columnName || columnName.Split(".").Last() == c.ColumnName);

                    if (dataColumn != null)
                    {
                        primaryKeyValues.Add(dataRow[dataColumn]);
                    }
                }

                if (primaryKeyValues.Any())
                {
                    return primaryKeyValues;
                }

                return null;
            }
        }

        private Dictionary<int, ModifiedRow> _ModifiedRows = new Dictionary<int, ModifiedRow>();
        [JsonIgnore]
        /// <summary>
        /// Indicates the rows that have been modified in an editable grid. The property can be accessed when using CustomisationPlugin to implement custom update validation.
        /// </summary>
        /// <returns>Dictionary<int, ModifiedRow></returns>
        public Dictionary<int, ModifiedRow> ModifiedRows
        {
            get
            {
                var formValues = FormValues ?? new Dictionary<string, List<string>>();
                if (_ModifiedRows.Keys.Any() || formValues.Any() == false)
                {
                    return _ModifiedRows;
                }
                _ModifiedRows = new Dictionary<int, ModifiedRow>();
                var rowCount = formValues[FirstEditableColumnName].Count;

                for (var r = 0; r < rowCount; r++)
                {
                    if (RowsModified != null && RowsModified.Count == rowCount)
                    {
                        if (RowsModified[r].Modified)
                        {
                            _ModifiedRows[r] = RowsModified[r];
                        }
                    }
                }

                return _ModifiedRows;
            }
        }
        /// <summary>
        /// Use this method or the ClientEvents property to assign the name of a client-side JavaScript function to be executed for the specified client event.
        /// </summary>
        /// <param name="GridClientEvent clientEvent">The type of event</param>
        /// <param name="string functionName ">The name of the JavaScript method</param>
        public void Bind(GridClientEvent clientEvent, string functionName)
        {
            ClientEvents[clientEvent] = functionName;
        }
     
    }
}