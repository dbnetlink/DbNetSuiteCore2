using DbNetSuiteCore.Enums;
using Microsoft.AspNetCore.Html;

namespace DbNetSuiteCore.Models
{
    public class JsonType
    {
        public string TypeName { get; set; }
        public JsonType(Type type)
        {
            TypeName = $"{type.FullName}, {type.Assembly.FullName}";
        }
    }
}
