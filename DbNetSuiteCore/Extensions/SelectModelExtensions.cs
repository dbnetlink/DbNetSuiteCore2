using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Enums;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Data;
using System.Text.Json;
using DbNetSuiteCore.Helpers;

namespace DbNetSuiteCore.Extensions
{
    public static class SelectModelExtensions
    {
        public static QueryCommandConfig BuildQuery(this SelectModel selectModel)
        {
            string sql = $"select {ComponentModelExtensions.Top(selectModel)}{AddSelectPart(selectModel)} from {selectModel.TableName}";
            QueryCommandConfig query = new QueryCommandConfig(sql);

            selectModel.AddFilterPart(query);
            selectModel.AddOrderPart(query);

            query.Sql = $"{query.Sql}{ComponentModelExtensions.Limit(selectModel)}";
            return query;
        }

        public static QueryCommandConfig BuildProcedureCall(this SelectModel selectModel)
        {
            QueryCommandConfig query = new QueryCommandConfig($"{selectModel.ProcedureName}");
            AssignParameters(query, selectModel.ProcedureParameters);
            return query;
        }

        public static void AssignParameters(QueryCommandConfig query, List<DbParameter> parameters)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.Value is JsonElement)
                {
                    parameter.Value = JsonElementExtension.Value((JsonElement)parameter.Value);
                }
                query.Params[DbHelper.ParameterName(parameter.Name)] = GridColumnModelExtensions.TypedValue(parameter.TypeName, parameter.Value);
            }
        }

        public static QueryCommandConfig BuildEmptyQuery(this SelectModel selectModel)
        {
            return new QueryCommandConfig($"select {GetColumnExpressions(selectModel)} from {selectModel.TableName} where 1=2");
        }

        private static string AddSelectPart(this SelectModel selectModel)
        {
            List<string> selectPart = new List<string>();

            return string.Join(",", selectPart);
        }

        private static string GetColumnExpressions(this SelectModel selectModel)
        {
            return selectModel.Columns.Any() ? string.Join(",", selectModel.Columns.Select(x => x.Expression).ToList()) : "*";
        }


        private static void AddFilterPart(this SelectModel selectModel, QueryCommandConfig query)
        {
        }

        private static void AddOrderPart(this SelectModel selectModel, QueryCommandConfig query)
        {

        }
    }
}