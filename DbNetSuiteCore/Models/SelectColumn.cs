using DbNetSuiteCore.Enums;
using MongoDB.Bson;
using System.Data;

namespace DbNetSuiteCore.Models
{
    public class SelectColumn : ColumnModel
    {
        /// <summary>
        /// Indicates that the column should be used to group the value options in the select
        /// </summary>
        public bool OptionGroup { get; set; } = false;

        public SelectColumn()
        {
        }
        public SelectColumn(string expression) : base(expression)
        {
        }

        internal SelectColumn(DataColumn dataColumn, DataSourceType dataSourceType) : base(dataColumn, dataSourceType)
        {
        }

        internal SelectColumn(DataRow dataRow, DataSourceType dataSourceType) : base(dataRow, dataSourceType)
        {
        }
        internal SelectColumn(BsonElement element) : base(element)
        {
        }
    }
}
