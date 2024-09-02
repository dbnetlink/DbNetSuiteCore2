using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.Extensions
{
    public static class GridColumnModelExtensions
    {
        public static object? FormatValue(this GridColumnModel gridColumnModel, object value)
        {
            if (string.IsNullOrEmpty(value?.ToString()) || value == DBNull.Value)
            {
                return value;
            }

            if (string.IsNullOrEmpty(gridColumnModel.Format))
            {
                return value;
            }

            string format = gridColumnModel.Format;

            switch (format)
            {
                case "email":
                    value = $"<a href=\"mailto:{value}\">{value}</a>";
                    break;
                case "www":
                    value = $"<a target=\"_blank\" href=\"{value}\">{value}</a>";
                    break;
                default:
                    if (gridColumnModel?.DataType == typeof(DateTime))
                    {
                        value = Convert.ToDateTime(value).ToString(format);
                    }
                    if (gridColumnModel?.DataType == typeof(Int64))
                    {
                        value = Convert.ToInt64(value).ToString(format);
                    }
                    if (gridColumnModel?.DataType == typeof(Double))
                    {
                        value = Convert.ToDouble(value).ToString(format);
                    }

                    break;
            }

            return value;
        }
    }
}
