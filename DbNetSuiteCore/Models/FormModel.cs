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
        public List<string> LinkedFormIds => GetLinkedControlIds(nameof(FormModel));
        public Dictionary<FormClientEvent, string> ClientEvents { get; set; } = new Dictionary<FormClientEvent, string>();
        public IEnumerable<FormColumn> Columns { get; set; } = new List<FormColumn>();
        internal override FormColumn? SortColumn => null;
        internal override SortOrder? SortSequence { get; set; }
        public int CurrentRecord { get; set; } = 1;
        public ToolbarPosition ToolbarPosition { get; set; } = ToolbarPosition.Bottom;
        public List<List<object>> PrimaryKeyValues { get; set; } = new List<List<object>>();
        [JsonIgnore]
        public Dictionary<string, string> FormValues = new Dictionary<string, string>();
        public bool Insert { get; set; } = false;
        public bool Delete { get; set; } = false;
        public FormMode Mode { get; set; } = FormMode.Empty;
        public bool ValidationPassed { get; set; } = false;
        public bool OneToOne { get; set; } = false;
        public bool ReadOnly { get; set; } = false;
        public FormMode? CommitType { get; set; }
        public object? RecordId => Mode == FormMode.Update ? PrimaryKeyValues[CurrentRecord - 1] : null;
        public int LayoutColumns { get; set; } = 4;
        public override IEnumerable<ColumnModel> SearchableColumns => GetColumns().Where(c => c.StringSearchable);
        public ModifiedRow Modified { get; set; } = new ModifiedRow();
        public Dictionary<string, object[]> RecordData => Data.Columns.Cast<DataColumn>().Where(c => c.DataType != typeof(Byte[])).ToDictionary(c => c.ColumnName, c => Data.Rows.Cast<DataRow>().AsEnumerable().Select(r => r[c]).ToArray());
        public ICustomForm? CustomisationClass
        {
            set { CustomisationTypeName = SetType(value); }
        }
        internal Type? GetCustomisationType => GetType(CustomisationTypeName);
        public string CustomisationTypeName { get; set; } = string.Empty;

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

        public override IEnumerable<ColumnModel> GetColumns()
        {
            return Columns.Cast<ColumnModel>();
        }

        public override void SetColumns(IEnumerable<ColumnModel> columns)
        {
            Columns = columns.Cast<FormColumn>();
        }

        public override ColumnModel NewColumn(DataRow dataRow, DataSourceType dataSourceType)
        {
            return new FormColumn(dataRow, dataSourceType);
        }
        public override ColumnModel NewColumn(DataColumn dataColumn, DataSourceType dataSourceType)
        {
            return new FormColumn(dataColumn, dataSourceType) { Required = dataColumn.AllowDBNull == false};
        }
        public override ColumnModel NewColumn(BsonElement element)
        {
            return new FormColumn(element) { Autoincrement = element.Name == MongoDbRepository.PrimaryKeyName };
        }

        public object FormValue(string columnName)
        {
            return FormValues.ContainsKey(columnName) ? FormValues[columnName] : Data.Rows[0][columnName];
        }

        public void Bind(FormClientEvent clientEvent, string functionName)
        {
            ClientEvents[clientEvent] = functionName;
        }
    }
}