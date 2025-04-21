using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using MongoDB.Bson;
using System.Data;

namespace DbNetSuiteCore.Models
{
    public class GridFormColumn : ColumnModel
    {
        private object? _minValue { get; set; } = null;
        private object? _maxValue { get; set; } = null;
        private bool _suggest = false;
        public FormControlType ControlType { get; set; } = FormControlType.Auto;
        public bool Required { get; set; } = false;
        public bool InError { get; set; } = false;
        public object? MinValue
        {
            get
            {
                if (_minValue != null)
                {
                    return _minValue;
                }
                switch (DbDataType)
                {
                    case nameof(MSSQLDataTypes.TinyInt):
                        _minValue = 0;
                        break;
                }
                return _minValue;
            }
            set { _minValue = value; }
        }
        public object? MaxValue
        {
            get
            {
                if (_maxValue != null)
                {
                    return _maxValue;
                }
                switch (DbDataType)
                {
                    case nameof(MSSQLDataTypes.TinyInt):
                        _maxValue = 255;
                        break;
                }
                return _maxValue;
            }
            set { _maxValue = value; }
        }
        public TextTransform? TextTransform { get; set; } = null;
        public int? MaxLength { get; set; } = null;
        public int? MinLength { get; set; } = null;
        public string? Pattern { get; set; } = null;

        public HtmlEditor? HtmlEditor { get; set; } = null;
        public GridFormColumn()
        {
        }
        public GridFormColumn(string name) : base(name, TextHelper.GenerateLabel(name))
        {
        }

        public GridFormColumn(string expression, string label) : base(expression, label)
        {
        }

        internal GridFormColumn(DataColumn dataColumn, DataSourceType dataSourceType) : base(dataColumn, dataSourceType)
        {
        }

        internal GridFormColumn(DataRow dataRow, DataSourceType dataSourceType) : base(dataRow, dataSourceType)
        {
        }

        internal GridFormColumn(BsonElement element) : base(element)
        {
        }
    }
}