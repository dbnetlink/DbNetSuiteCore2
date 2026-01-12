using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using System.Data;

namespace DbNetSuiteCore.Extensions
{
    public static class TreeModelExtensions
    {
        public static void AddFilterPart(this TreeModel treeModel, QueryCommandConfig query)
        {
            List<string> filterParts = new List<string>();

            if (!string.IsNullOrEmpty(treeModel.FixedFilter))
            {
                filterParts.Add($"({treeModel.FixedFilter})");
                ComponentModelExtensions.AssignParameters(query, treeModel.FixedFilterParameters);
            }

            if (filterParts.Any())
            {
                query.Sql += $" where {string.Join(" and ", filterParts)}";
            }
        }

        public static QueryCommandConfig BuildEmptyQuery(this TreeModel treeModel)
        {
            return new QueryCommandConfig(treeModel.DataSourceType) { Sql = $"select {GetColumnExpressions(treeModel)} from {treeModel.TableName} where 1=2" };
        }


        private static string GetColumnExpressions(this TreeModel treeModel)
        {
            return treeModel.Columns.Any() ? string.Join(",", treeModel.Columns.Select(x => x.Expression).ToList()) : "*";
        }

        public static void AddOrderPart(this TreeModel treeModel, QueryCommandConfig query)
        {
            string optionGroupOrdinal = string.Empty; 
            query.Sql += $" order by {optionGroupOrdinal}{treeModel.SortColumnOrdinal} {treeModel.SortSequence}";
        }
    }
}
