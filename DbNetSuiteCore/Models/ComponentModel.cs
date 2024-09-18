using System.Text.Json.Serialization;
using System.Data;

namespace DbNetSuiteCore.Models
{
    public class ComponentModel
    {
         [JsonIgnore]
        public DataTable Data { get; set; } = new DataTable();
    }
}