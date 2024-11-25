using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using System.Data;

namespace DbNetSuiteCore.Models
{
    public class FormColumn : ColumnModel
    {
        public FormControlType FormControlType { get; set; } = FormControlType.Auto;
        public bool Required { get; set; } = false;
        public bool InError { get; set; } = false;
        public bool ReadOnly { get; set; } = false;
        public bool Disabled { get; set; } = false;
        public object InitialValue { get; set; } = string.Empty;

        public FormColumn()
        {
        }
        public FormColumn(string name) : base(name, TextHelper.GenerateLabel(name))
        {
        }

        public FormColumn(string expression, string label) : base(expression, label)
        {
        }

        internal FormColumn(DataColumn dataColumn, DataSourceType dataSourceType) : base(dataColumn, dataSourceType)
        {
        }

        internal FormColumn(DataRow dataRow) : base(dataRow)
        {
        }
    }
}
