using DbNetTimeCore.Models;
using static DbNetTimeCore.Utilities.DbNetDataCore;


namespace Microsoft.AspNetCore.Mvc
{
    public static class GridModelExtensions
    {
        public static QueryCommandConfig BuildQuery(this GridModel gridModel)
        {
            string sql = $"select {gridModel.GetColumnExpressions()} from {gridModel.TableName}";
            QueryCommandConfig query = new QueryCommandConfig(sql);

            gridModel.AddFilterPart(query);

            if (gridModel is GridModel)
            {
                if (!string.IsNullOrEmpty(gridModel.SortKey) || !string.IsNullOrEmpty(gridModel.CurrentSortKey))
                {
                    gridModel.AddOrderPart(query);
                }
            }

            return query;
        }
        public static QueryCommandConfig BuildEmptyQuery(this GridModel gridModel)
        {
            return new QueryCommandConfig($"select {GetColumnExpressions(gridModel)} from {gridModel.TableName} where 1=2");
        }

        private static void AddFilterPart(this GridModel gridModel, CommandConfig query)
        {
            if (string.IsNullOrEmpty(gridModel.SearchInput))
            {
                return;
            }
            List<string> filterPart = new List<string>();

            foreach (var gridColumn in gridModel.GridColumns.Where(c => c.Searchable))
            {
                query.Params[$"@{gridColumn.ParamName}"] = $"%{gridModel.SearchInput}%";
                filterPart.Add($"{gridColumn.Expression.Split(" ").First()} like @{gridColumn.ParamName}");
            }

            if (filterPart.Any())
            {
                query.Sql += $" where {string.Join(" or ", filterPart)}";
            }
        }

        private static void AddOrderPart(this GridModel gridModel, QueryCommandConfig query)
        {
            query.Sql += $" order by {(!string.IsNullOrEmpty(gridModel.SortKey) ? gridModel.SortColumn : gridModel.CurrentSortColumn)} {gridModel.SortSequence}";
        }

        private static string GetColumnExpressions(this GridModel gridModel)
        {
            return gridModel.GridColumns.Any() ? string.Join(",", gridModel.GridColumns.Select(x => x.Expression).ToList()) : "*";
        }
    }
}
