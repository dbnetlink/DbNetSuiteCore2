using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using System.Data;

namespace DbNetSuiteCore.Extensions
{
    public static class SelectModelExtensions
    {
        public static void AddFilterPart(this SelectModel selectModel, QueryCommandConfig query)
        {
            List<string> filterParts = new List<string>();
            if (string.IsNullOrEmpty(selectModel.SearchInput) == false)
            {
                List<string> quickSearchFilterPart = new List<string>();
                var searchColumn = selectModel.Columns.Count() == 1 ? selectModel.Columns.First() : selectModel.Columns.Skip(1).First();
                ComponentModelExtensions.AddSearchFilterPart(selectModel, searchColumn, query, quickSearchFilterPart);

                if (quickSearchFilterPart.Any())
                {
                    filterParts.Add($"({string.Join(" or ", quickSearchFilterPart)})");
                }
            }

            if (selectModel.IsLinked)
            {
                if (!string.IsNullOrEmpty(selectModel.ParentKey))
                {
                    var foreignKeyColumn = selectModel.Columns.FirstOrDefault(c => c.ForeignKey);
                    if (foreignKeyColumn != null)
                    {
                        filterParts.Add($"({foreignKeyColumn.Expression.Split(" ").First()} = @{foreignKeyColumn.ParamName})");
                        query.Params[$"@{foreignKeyColumn.ParamName}"] = foreignKeyColumn!.TypedValue(selectModel.ParentKey) ?? string.Empty;
                    }
                }
                else
                {
                    filterParts.Add($"(1=2)");
                }
            }

            if (filterParts.Any())
            {
                query.Sql += $" where {string.Join(" and ", filterParts)}";
            }

        }

        public static QueryCommandConfig BuildEmptyQuery(this SelectModel selectModel)
        {
            return new QueryCommandConfig($"select {GetColumnExpressions(selectModel)} from {selectModel.TableName} where 1=2");
        }


        private static string GetColumnExpressions(this SelectModel selectModel)
        {
            return selectModel.Columns.Any() ? string.Join(",", selectModel.Columns.Select(x => x.Expression).ToList()) : "*";
        }

        private static void AddOrderPart(this SelectModel selectModel, QueryCommandConfig query)
        {

        }
    }
}