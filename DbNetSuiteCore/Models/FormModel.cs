using DbNetSuiteCore.Constants;
using DbNetSuiteCore.CustomisationHelpers.Interfaces;
using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Repositories;
using DocumentFormat.OpenXml.Spreadsheet;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.Data;

namespace DbNetSuiteCore.Models
{
    public class FormModel : ComponentModel
    {
        [JsonProperty]
        internal List<string> LinkedFormIds => GetLinkedControlIds(nameof(FormModel));
        /// <summary>
        /// Use this property or the Bind method to assign the name of a client-side JavaScript function to be executed for the specified client event.
        /// </summary>
        public Dictionary<FormClientEvent, string> ClientEvents { get; set; } = new Dictionary<FormClientEvent, string>();
        /// <summary>
        /// Defines the columns that should appear in the form.
        /// </summary>
        public IEnumerable<FormColumn> Columns { get; set; } = new List<FormColumn>();
        internal override FormColumn? SortColumn => null;
        internal override SortOrder? SortSequence { get; set; }
        [JsonProperty]
        internal int CurrentRecord { get; set; } = 1;
        /// <summary>
        /// Defines the location of the form toolbar.
        /// </summary>
        public ToolbarPosition ToolbarPosition { get; set; } = ToolbarPosition.Bottom;
        [JsonProperty]
        internal List<List<object>> PrimaryKeyValues { get; set; } = new List<List<object>>();
        [JsonIgnore]
        public Dictionary<string, string> FormValues = new Dictionary<string, string>();
        /// <summary>
        /// When set to true allows new records to be inserted via the form.
        /// </summary>
        public bool Insert { get; set; } = false;
        /// <summary>
        /// When set to true allows existing records to be deleted via the form.
        public bool Delete { get; set; } = false;
        [JsonProperty]
        internal FormMode Mode { get; set; } = FormMode.Empty;
        [JsonProperty]
        internal bool OneToOne { get; set; } = false;
        /// <summary>
        /// Makes the entire form read only.
        /// </summary>
        public bool ReadOnly { get; set; } = false;
        [JsonProperty]
        internal FormMode? CommitType { get; set; }
        [JsonProperty]
        internal object? RecordId => Mode == FormMode.Update ? PrimaryKeyValues[CurrentRecord - 1] : null;
        /// <summary>
        /// Controls the number of columns in the form layout
        /// </summary>
        /// <remarks>
        /// Used in conjunction with the FormColumn ColSpan and RowSpan properties to configure the layout of the form
        /// </remarks>
        public int LayoutColumns { get; set; } = 4;
        internal override IEnumerable<ColumnModel> SearchableColumns => GetColumns().Where(c => c.StringSearchable);
        /// <summary>
        /// Provides information on the columns that have been modified. 
        /// </summary>
        public ModifiedRow Modified { get; set; } = new ModifiedRow();
        internal Dictionary<string, object[]> RecordData => Data.Columns.Cast<DataColumn>().Where(c => c.DataType != typeof(Byte[])).ToDictionary(c => c.ColumnName, c => Data.Rows.Cast<DataRow>().AsEnumerable().Select(r => r[c]).ToArray());
        /// <summary>
        /// Use this property to specify the type of a class that implements the ICustomFormPlugin interface which at runtime will be instantiated and used to provide runtime initialisation customisation and/or custom server-side validation for forms.
        /// </summary>
        public Type? CustomisationPlugin
        {
            set { CustomisationPluginName = PluginHelper.GetNameFromType(value); }
        }
        [JsonProperty]
        internal string CustomisationPluginName { get; set; } = string.Empty;

        public FormModel() : base()
        {
        }
        public FormModel(DataSourceType dataSourceType, string url) : base(dataSourceType, url)
        {
        }

        public FormModel(DataSourceType dataSourceType, string connectionAlias, string tableName) : base(dataSourceType, connectionAlias, tableName)
        {
        }

        public FormModel(string tableName) : base(tableName)
        {
        }

        internal override IEnumerable<ColumnModel> GetColumns()
        {
            return Columns.Cast<ColumnModel>();
        }

        internal override void SetColumns(IEnumerable<ColumnModel> columns)
        {
            Columns = columns.Cast<FormColumn>();
        }

        internal override ColumnModel NewColumn(DataRow dataRow, DataSourceType dataSourceType)
        {
            return new FormColumn(dataRow, dataSourceType);
        }
        internal override ColumnModel NewColumn(DataColumn dataColumn, DataSourceType dataSourceType)
        {
            return new FormColumn(dataColumn, dataSourceType) { Required = dataColumn.AllowDBNull == false};
        }
        internal override ColumnModel NewColumn(BsonElement element)
        {
            return new FormColumn(element) { Autoincrement = element.Name == MongoDbRepository.PrimaryKeyName };
        }
        /// <summary>
        /// Provides access to current form value for a specified column. Can be used with CustomisationPlugin property to provide custom server-side validation for the form.
        /// </summary>
        /// <param name="columnName">Name of the column</param>
        /// <returns>Current value for the column</returns>
        public object FormValue(string columnName)
        {
            return FormValues.ContainsKey(columnName) ? FormValues[columnName] : Data.Rows[0][columnName];
        }
        /// <summary>
        /// Use this method or the ClientEvents property to assign the name of a client-side JavaScript function to be executed for the specified client event.
        /// </summary>
        /// <param name="clientEvent">Type of client-side event</param>
        /// <param name="functionName">Name of the JavaScript function</param>
        public void Bind(FormClientEvent clientEvent, string functionName)
        {
            ClientEvents[clientEvent] = functionName;
        }
    }
}