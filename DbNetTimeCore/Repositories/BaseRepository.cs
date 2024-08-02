using DbNetTimeCore.Models;
using System.Data;

namespace DbNetTimeCore.Repositories
{
    public class BaseRepository
    { 
        protected string GetColumnExpressions(GridModel gridModel)
        {
            return gridModel.GridColumns.Any() ? string.Join(",", gridModel.GridColumns.Select(x => x.Expression).ToList()) : "*";
        }
    }

    public class CommandConfig
    {
        public string Sql = String.Empty;
        public Dictionary<string, object> Params = new Dictionary<string, object>();

        public CommandConfig()
            : this("")
        {
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
    public class QueryCommandConfig : CommandConfig
    {
        public CommandBehavior Behavior = CommandBehavior.Default;

        public QueryCommandConfig()
            : this("")
        {
        }
        public QueryCommandConfig(string sql)
            : base(sql)
        {
        }
        public QueryCommandConfig(string sql, Dictionary<string, object> parameters) : base(sql, parameters)
        {
        }
    }
    public class UpdateCommandConfig : CommandConfig
    {
        public Dictionary<string, object> FilterParams = new Dictionary<string, object>();

        public UpdateCommandConfig()
            : this("")
        {
        }
        public UpdateCommandConfig(string Sql)
            : base(Sql)
        {
        }
    }
}
