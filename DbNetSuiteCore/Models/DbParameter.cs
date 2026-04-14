using System.Text.Json.Serialization;

namespace DbNetSuiteCore.Models
{
    public class DbParameter
    {
        private Type _type = null;
        public string Name { get; set; } = string.Empty;
        public object Value { get; set; } = string.Empty;
        [JsonIgnore]
        public Type Type {
            get 
            {
                return _type ?? Type.GetType($"System.{TypeName}") ?? typeof(String);
            }
            set {
                _type = value;
                TypeName = value.GetType().Name;
            } 
        }
        public string TypeName { get; set; } = string.Empty;
        public DbParameter() { }

        public DbParameter(string name, object value)
        {
            Name = name;
            Value = value;
            Type = value.GetType();
        }
        public DbParameter(string name, object value, Type type)
        {
            Name = name;
            Value = value;
            Type = type;
        }
    }
}
