using DbNetSuiteCore.Enums;
using MongoDB.Bson;
using System.Data;

namespace DbNetSuiteCore.Models
{
    public class TreeColumn : ColumnModel
    {
        /// <summary>
        /// Indicates that the column should be used to group the value options in the select
        /// </summary>
        public bool OptionGroup { get; set; } = false;

        public TreeColumn()
        {
        }
        public TreeColumn(string expression) : base(expression)
        {
        }

        internal TreeColumn(DataColumn dataColumn, DataSourceType dataSourceType) : base(dataColumn, dataSourceType)
        {
        }

        internal TreeColumn(DataRow dataRow, DataSourceType dataSourceType) : base(dataRow, dataSourceType)
        {
        }
        internal TreeColumn(BsonElement element) : base(element)
        {
        }
    }
}
