using DbNetSuiteCore.Enums;
using System.Data;
using MongoDB.Bson;
using DbNetSuiteCore.Constants;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Collections.Generic;
using DbNetSuiteCore.Helpers;
using Newtonsoft.Json;
using DbNetSuiteCore.CustomisationHelpers.Interfaces;

namespace DbNetSuiteCore.Models
{
    public class GridModel : ComponentModel
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
        internal override GridColumn? SortColumn => (Columns.FirstOrDefault(c => c.Key == CurrentSortKey) ?? CurrentSortColumn) ?? InitalSortColumn;
        internal GridColumn? CurrentSortColumn => Columns.FirstOrDefault(c => c.Key == CurrentSortKey);
        internal GridColumn? InitalSortColumn => Columns.FirstOrDefault(c => c.InitialSortOrder.HasValue) ?? Columns.FirstOrDefault(c => c.Sortable);
        internal override SortOrder? SortSequence 
        { 
            get { return _SortSequence == null ? (Columns.FirstOrDefault(c => c.InitialSortOrder.HasValue)?.InitialSortOrder ?? SortOrder.Asc) : _SortSequence; } 
            set { _SortSequence = value; } 
        }
        internal string ExportFormat { get; set; } = string.Empty;
        internal List<string> ColumnFilter { get; set; } = new List<string>();
        public int PageSize { get; set; } = 20;
        [JsonProperty]
        internal bool IsNested { get; set; } = false;
        public bool IncludeJsonData { get; set; } = false;
        [JsonIgnore]
        public string JsonData { get; set; } = string.Empty;
        public int ColSpan => VisbleColumns.Count();
        internal override IEnumerable<ColumnModel> SearchableColumns => GetColumns().Where(c => c.StringSearchable);
        [JsonProperty]
        internal List<string> LinkedGridIds => GetLinkedControlIds(nameof(GridModel));
        public List<GridModel> _NestedGrids { get; set; } = new List<GridModel>();
        internal bool HasNestedGrids => _NestedGrids.Any();
        public IEnumerable<GridColumn> Columns { get; set; } = new List<GridColumn>();
        public Dictionary<GridClientEvent, string> ClientEvents { get; set; } = new Dictionary<GridClientEvent, string>();
        public bool OptimizeForLargeDataset { get; set; } = false;
        internal bool PaginateQuery => OptimizeForLargeDataset && TriggerName != TriggerNames.Download;
        [JsonProperty]
        internal int TotalRows { get; set; }
        internal bool IsGrouped => Columns.Any(c => c.Aggregate != AggregateType.None);
        internal bool IsEditable => Columns.Any(c => c.Editable);
        [JsonProperty]
        public Dictionary<string, List<string>> FormValues { get; internal set; } = new Dictionary<string, List<string>>();
        internal string FirstEditableColumnName => Columns.Where(c => c.Editable).First().ColumnName;
        [JsonIgnore]
        internal IEnumerable<DataRow> Rows => OptimizeForLargeDataset? Data.AsEnumerable() : Data.AsEnumerable().Skip((CurrentPage - 1) * PageSize).Take(PageSize);
        [JsonProperty]
        internal List<object> PrimaryKeyValues => Rows.Select(row => PrimaryKeyValue(row) ?? DBNull.Value).ToList();
        [JsonProperty]
        internal List<ModifiedRow>? RowsModified { get; set; } = new List<ModifiedRow>();
        [JsonIgnore]

        public Type? JsonTransformPlugin
        {
            set 
            {
                JsonTransformPluginName = PluginHelper.GetNameFromType(value);
            }
        }

        [JsonProperty]
        internal string JsonTransformPluginName { get; set; } = string.Empty;
        public Type? CustomisationPlugin
        {
            set{ CustomisationPluginName = PluginHelper.GetNameFromType(value);}
        }
        [JsonProperty]
        internal string CustomisationPluginName { get; set; } = string.Empty;

        public string JsonArrayProperty { get; set; } = string.Empty;
        [JsonIgnore]
        public GridModel NestedGrid
        {
            set
            {
                AddNestedGrid(value);
            }
        }

        public ToolbarPosition ToolbarPosition { get; set; } = ToolbarPosition.Top;
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
        public MultiRowSelectLocation MultiRowSelectLocation { get; set; } = MultiRowSelectLocation.None;
        public HeadingMode HeadingMode { get; set; } = HeadingMode.Normal;
        public ViewDialog? ViewDialog { get; set; }
        public GridModel() : base()
        {
        }
        public GridModel(DataSourceType dataSourceType, string url) :base(dataSourceType,url)
        {
        }

        public GridModel(DataSourceType dataSourceType, string connectionAlias, string tableName, bool isStoredProcedure = false) : base(dataSourceType, connectionAlias, tableName, isStoredProcedure)
        {
        }

        public GridModel(DataSourceType dataSourceType, string connectionAlias, string procedureName, List<DbParameter> procedureParameters) : base(dataSourceType, connectionAlias, procedureName, procedureParameters)
        {
        }

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

        public void AddNestedGrid(GridModel gridModel)
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

        internal object? PrimaryKeyValue(DataRow dataRow)
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

        public void Bind(GridClientEvent clientEvent, string functionName)
        {
            ClientEvents[clientEvent] = functionName;
        }
     
    }
}