using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Repositories;
using MongoDB.Bson;
using System.Data;
using System.Text.Json.Serialization;

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
        public List<object> PrimaryKeyValues { get; set; } = new List<object>();
        [JsonIgnore]
        public Dictionary<string, string> FormValues = new Dictionary<string, string>();
        public string Message = string.Empty;
        public MessageType MessageType = MessageType.None;
        public bool Insert { get; set; } = false;
        public bool Delete { get; set; } = false;
        public FormMode Mode { get; set; } = FormMode.Empty;
        public bool ValidationPassed { get; set; } = false;
        public bool OneToOne => IsLinked && Columns.Any(c => c.PrimaryKey && c.ForeignKey);
        public bool ReadOnly { get; set; } = false;
        public FormMode? CommitType { get; set; }
        public object RecordId => Mode == FormMode.Update ? PrimaryKeyValues[CurrentRecord - 1] : string.Empty;
        public int LayoutColumns { get; set; } = 4;
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

        public void Bind(FormClientEvent clientEvent, string functionName)
        {
            ClientEvents[clientEvent] = functionName;
        }
    }
}