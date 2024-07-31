using DbNetTimeCore.Models;

namespace DbNetTimeCore.Repositories
{
    public class BaseRepository
    { 
        protected string GetColumnExpressions(GridModel gridModel)
        {
            return gridModel.GridColumns.Any() ? string.Join(",", gridModel.GridColumns.Select(x => x.Expression).ToList()) : "*";
        }
    }
}
