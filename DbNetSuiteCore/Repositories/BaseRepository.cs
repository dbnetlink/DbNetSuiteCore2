using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Models;
using System.Data;

namespace DbNetSuiteCore.Repositories
{

    public record CommandConfig
    {
        public string Sql = String.Empty;
        public DataSourceType DataSourceType = DataSourceType.MSSQL;
        public Dictionary<string, object> Params = new Dictionary<string, object>();

        public CommandConfig()
        {
        }

        public CommandConfig(DataSourceType dataSourceType)
        {
            DataSourceType = dataSourceType;
        }

        public CommandConfig(string sql)
        {
            this.Sql = sql;
        }
        public CommandConfig(string sql, Dictionary<string, object> parameters)
        {
            this.Sql = sql;
            this.Params = parameters;
        }
    }
    public record QueryCommandConfig : CommandConfig
    {
        public CommandBehavior Behavior = CommandBehavior.Default;

        public QueryCommandConfig() : base()
        {
        }
        public QueryCommandConfig(DataSourceType dataSourceType)
            : base(dataSourceType)
        {
        }
    }
    public record UpdateCommandConfig : CommandConfig
    {
        public Dictionary<string, object> FilterParams = new Dictionary<string, object>();

        public UpdateCommandConfig(DataSourceType dataSourceType)
            : base(dataSourceType)
        {
        }
      
    }
}
