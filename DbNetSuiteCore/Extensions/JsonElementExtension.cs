using System.Text.Json;

namespace DbNetSuiteCore.Extensions
{
    public static class JsonElementExtension
    {
        public static object Value(this JsonElement jsonElement)
        {
            object value;
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.String:
                    value = jsonElement.GetString();
                    break;
                case JsonValueKind.Number:
                    value = jsonElement.GetUInt64();
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    value = jsonElement.GetBoolean();
                    break;
                case JsonValueKind.Null:
                    value = DBNull.Value;
                    break;
                default:
                    value = jsonElement.ToString();
                    break;
            }
            return value;
        }
    }
}
