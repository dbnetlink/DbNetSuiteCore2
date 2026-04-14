using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.Helpers
{
    public static class ColumnsHelper
    {
        public static IEnumerable<ColumnModel> MoveDataOnlyColumnsToEnd(IEnumerable<ColumnModel> columns)
        {
            if (columns.Any(c => c.DataOnly))
            {
                List<ColumnModel> newColumns = columns.Where(c => c.DataOnly == false).ToList();
                newColumns.AddRange(columns.Where(c => c.DataOnly).ToList());
                columns = newColumns;
            }

            return columns;
        }

        public static string GetColumnExpressions(IEnumerable<ColumnModel> columns)
        {
            return columns.Any() ? string.Join(",", columns.Select(x => x.Expression).ToList()) : "*";
        }

        public static void QualifyColumnExpressions(IEnumerable<ColumnModel> columns, DataSourceType dataSourceType)
        {
            columns.ToList().ForEach(c => c.Expression = DbHelper.QualifyExpression(c.Expression, dataSourceType));
        }

        public static void CopyPropertiesTo<T, TU>(this T source, TU dest)
        {
            var sourceProps = typeof(T).GetProperties().Where(x => x.CanRead).ToList();
            var destProps = typeof(TU).GetProperties().Where(x => x.CanWrite).ToList();

            foreach (var sourceProp in sourceProps)
            {
                if (destProps.Any(x => x.Name == sourceProp.Name))
                {
                    var p = destProps.First(x => x.Name == sourceProp.Name);
                    if (p.CanWrite)
                    { // check if the property can be set or no.
                        p.SetValue(dest, sourceProp.GetValue(source, null), null);
                    }
                }
            }
        }
    }
}
