using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Plugins.Interfaces;
using Newtonsoft.Json;
namespace DbNetSuiteCore.Models
{
    public abstract class GridSelectModel : ComponentModel
    {
        private bool _cache = false;
        private string _cacheKey = string.Empty;

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
        /// <summary>
        /// When set to true the data retrieved from the data source will be cached for subsequent requests. Only valid for Excel and JSON data sources.
        /// </summary>
        public bool Cache
        {
            get
            {
                return _cache || string.IsNullOrEmpty(_cacheKey) == false;
            }
            set
            {
                _cache = value;
            }
        }
        /// <summary>
        /// Enable caching across multiple instances of the component dataset using the supplied unique key (typicaly a GUID).
        /// </summary>
        public string CacheKey
        {
            get
            {
                return string.IsNullOrEmpty(_cacheKey) ? Id : _cacheKey;
            }
            set
            {
                _cacheKey = value;
            }
        }

        public GridSelectModel() : base()
        {
        }

        public GridSelectModel(DataSourceType dataSourceType, string url) : base(dataSourceType, url)
        {
        }

        public GridSelectModel(DataSourceType dataSourceType, Type dataSourcePlugin) : base(dataSourceType, dataSourcePlugin)
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