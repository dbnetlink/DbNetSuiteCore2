using System.Data;

namespace DbNetSuiteCore.Extensions
{
    public static class DataSetExtensions
    {
        public static DataTable GetTable(this DataSet dataSet, string? tableName)
        {
            DataTable dataTable = dataSet.Tables[0];
            if (string.IsNullOrEmpty(tableName) == false)
            {
                DataTable? dt = dataSet.Tables[tableName];
                if (dt != null)
                {
                    dataTable = dt;
                }
            }

            return dataTable;
        }
    }
}
