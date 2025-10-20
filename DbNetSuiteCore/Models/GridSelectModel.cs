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

        public GridSelectModel() : base()
        {
        }

        public GridSelectModel(DataSourceType dataSourceType, string url) : base(dataSourceType, url)
        {
        }

        public GridSelectModel(DataSourceType dataSourceType, string connectionAlias, string tableName, bool isStoredProcedure = false) : base(dataSourceType, connectionAlias, tableName, isStoredProcedure)
        {
        }

        public GridSelectModel(DataSourceType dataSourceType, string connectionAlias, string procedureName, List<DbParameter> procedureParameters) : base(dataSourceType, connectionAlias, procedureName, procedureParameters)
        {
        }

        public GridSelectModel(string tableName) : base(tableName)
        {
        }
    }
}