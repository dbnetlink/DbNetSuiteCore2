using DbNetSuiteCore.Enums;
using DocumentFormat.OpenXml.Drawing.Charts;
using MongoDB.Bson;
using Newtonsoft.Json;
using System.Data;
namespace DbNetSuiteCore.Models
{
    public abstract class GridSelectModel : ComponentModel
    {
        /// <summary>
        /// Allows custom headers to be added to API requests when using an API JSON data source.
        /// </summary>
        public Dictionary<string, string> ApiRequestHeaders { get; set; } = new Dictionary<string, string>();
        /// <summary>
        /// Allows custom request parameters to be added to API requests when using an API JSON data source.
        /// </summary>
        public Dictionary<string, string> ApiRequestParameters { get; set; } = new Dictionary<string, string>();
        /// <summary>
        /// Name of stored procedure used for data source.
        /// </summary>
        public string ProcedureName { get; set; } = string.Empty;
        /// <summary>
        /// Names and values of any parameters required by the stored procedure
        /// </summary>
        /// <remarks>
        /// The Type of the parameter value is inferred by default but a specific type can be provided via the option Type property
        /// </remarks>
        public List<DbParameter> ProcedureParameters { get; set; } = new List<DbParameter>();
        [JsonProperty]
        internal bool IsStoredProcedure { get; set; } = false;
        public GridSelectModel() : base()
        {
        }

        public GridSelectModel(DataSourceType dataSourceType, string url) : base(dataSourceType, url)
        {
        }

        public GridSelectModel(DataSourceType dataSourceType, string connectionAlias, string tableName, bool isStoredProcedure = false)
        {
            DataSourceType = dataSourceType;
            ConnectionAlias = connectionAlias;
            TableName = isStoredProcedure ? string.Empty : tableName;
            ProcedureName = isStoredProcedure ? tableName : string.Empty;
            IsStoredProcedure = isStoredProcedure;
        }

        public GridSelectModel(DataSourceType dataSourceType, string connectionAlias, string procedureName, List<DbParameter> procedureParameters)
        {
            DataSourceType = dataSourceType;
            ConnectionAlias = connectionAlias;
            ProcedureName = procedureName;
            ProcedureParameters = procedureParameters;
            IsStoredProcedure = true;
        }

        public GridSelectModel(string tableName) : base(tableName)
        {
        }
    }
}