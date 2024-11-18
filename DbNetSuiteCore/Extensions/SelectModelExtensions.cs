using DbNetSuiteCore.Helpers;
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

                foreach (var searchColumn in selectModel.SearchableColumns )
                {
                    ComponentModelExtensions.AddSearchFilterPart(selectModel, searchColumn, query, quickSearchFilterPart);
                }

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
                        filterParts.Add($"({DbHelper.StripColumnRename(foreignKeyColumn.Expression)} = @{foreignKeyColumn.ParamName})");
                        query.Params[$"@{foreignKeyColumn.ParamName}"] = ColumnModelHelper.TypedValue(foreignKeyColumn,selectModel.ParentKey) ?? string.Empty;
                    }
                }
                else
                {
                    filterParts.Add($"(1=2)");
                }
            }

            if (!string.IsNullOrEmpty(selectModel.FixedFilter))
            {
                filterParts.Add($"({selectModel.FixedFilter})");
                ComponentModelExtensions.AssignParameters(query, selectModel.FixedFilterParameters);
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

        public static void AddOrderPart(this SelectModel selectModel, QueryCommandConfig query)
        {
            string optionGroupOrdinal = string.Empty; 

            if (selectModel.IsGrouped)
            {
                optionGroupOrdinal = $"{selectModel.OptionGroupColumn.Ordinal},";
            }
            query.Sql += $" order by {optionGroupOrdinal}{selectModel.SortColumnOrdinal} {selectModel.SortSequence}";
        }
    }
}
