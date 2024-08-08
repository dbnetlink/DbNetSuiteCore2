using TQ.Models;
using DbNetTimeCore.Repositories;


namespace Microsoft.AspNetCore.Mvc
{
    public static class GridModelExtensions
    {
        public static QueryCommandConfig BuildQuery(this GridModel gridModel)
        {
            string sql = $"select {gridModel.GetColumnExpressions()} from {gridModel.TableName}";
            QueryCommandConfig query = new QueryCommandConfig(sql);

            gridModel.AddFilterPart(query);
            gridModel.AddOrderPart(query);

            return query;
        }
        public static QueryCommandConfig BuildEmptyQuery(this GridModel gridModel)
        {
            return new QueryCommandConfig($"select {GetColumnExpressions(gridModel)} from {gridModel.TableName} where 1=2");
        }

        private static void AddFilterPart(this GridModel gridModel, CommandConfig query)
        {
            List<string> filterParts = new List<string>();

            if (!string.IsNullOrEmpty(gridModel.SearchInput))
            {
                List<string> quickSearchFilterPart = new List<string>();

                foreach (var gridColumn in gridModel.GridColumns.Where(c => c.Searchable))
                {
                    query.Params[$"@{gridColumn.ParamName}"] = $"%{gridModel.SearchInput}%";
                    quickSearchFilterPart.Add($"{gridColumn.Expression.Split(" ").First()} like @{gridColumn.ParamName}");
                }

                if (quickSearchFilterPart.Any())
                {
                    filterParts.Add($"({string.Join(" or ", quickSearchFilterPart)})");
                }
            }

            if (!string.IsNullOrEmpty(gridModel.FixedFilter))
            {
                filterParts.Add($"({gridModel.FixedFilter})");
            }

            if (filterParts.Any())
            {
                query.Sql += $" where {string.Join(" and ", filterParts)}";
            }
        }

        private static void AddOrderPart(this GridModel gridModel, QueryCommandConfig query)
        {
            if (string.IsNullOrEmpty(gridModel.CurrentSortKey))
            {
                gridModel.SetInitialSort();
            }
            query.Sql += $" order by {(!string.IsNullOrEmpty(gridModel.SortKey) ? gridModel.SortColumn : gridModel.CurrentSortColumn)} {gridModel.SortSequence}";
        }

        public static void SetInitialSort(this GridModel gridModel)
        {
            gridModel.CurrentSortKey = gridModel.Columns.First().Key;
            gridModel.CurrentSortAscending = true;

            var initialSortOrderColumn = gridModel.Columns.FirstOrDefault(c => c.InitialSortOrder.HasValue);

            if (initialSortOrderColumn != null)
            {
                gridModel.CurrentSortKey = initialSortOrderColumn.Key;
                gridModel.CurrentSortAscending = initialSortOrderColumn.InitialSortOrder!.Value == DbNetTimeCore.Enums.SortOrder.Asc;
            }
        }

        private static string GetColumnExpressions(this GridModel gridModel)
        {
            return gridModel.GridColumns.Any() ? string.Join(",", gridModel.GridColumns.Select(x => x.Expression).ToList()) : "*";
        }
    }
}
