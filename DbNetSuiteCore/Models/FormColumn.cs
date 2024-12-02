using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Helpers;
using System.Data;

namespace DbNetSuiteCore.Models
{
    public class FormColumn : ColumnModel
    {
        private object? _minValue { get; set; } = null;
        private object? _maxValue { get; set; } = null;
        public FormControlType ControlType { get; set; } = FormControlType.Auto;
        public bool Required { get; set; } = false;
        public bool InError { get; set; } = false;
        public bool ReadOnly { get; set; } = false;
        public bool Disabled { get; set; } = false;
        public object InitialValue { get; set; } = string.Empty;
        public object? MinValue
        {
            get
            {
                if (_minValue != null)
                {
                    return _minValue;
                }
                switch (DbDataType.ToLower())
                {
                    case "tinyint":
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
                switch (DbDataType.ToLower())
                {
                    case "tinyint":
                        _maxValue = 255;
                        break;
                }
                return _maxValue;
            }
            set { _maxValue = value; }
        }
        public long? JSDateTime { get; set; } = null;
        public bool Autoincrement { get; set; } = false;
        public TextTransform? TextTransform { get; set; } = null;
        public int? MaxLength { get; set; } = null;
        public bool PrimaryKeyRequired => PrimaryKey && Autoincrement == false;

        public string DateTimeFormat => GetDateTimeFormat();

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

        private string GetDateTimeFormat()
        {
            switch (DataType.Name)
            {
                case nameof(DateTime):
                    return (ControlType == FormControlType.DateTime) ? "yyyy-MM-dd'T'HH:mm" : "yyyy-MM-dd";
                case nameof(DateTimeOffset):
                    return (ControlType == FormControlType.DateTime) ? "yyyy-MM-dd'T'HH:mm" : "yyyy-MM-dd";
                case nameof(TimeSpan):
                    return (ControlType == FormControlType.TimeWithSeconds) ? @"hh\:mm\:ss" : @"hh\:mm";
            }

            return string.Empty;
        }
    }
}
