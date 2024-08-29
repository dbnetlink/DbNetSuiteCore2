using System.Text.Json.Serialization;
using System.Data;

namespace DbNetSuiteCore.Models
{
    public class ComponentModel
    {
        public List<GridColumnModel> Columns { get; set; } = new List<GridColumnModel>();
        [JsonIgnore]
        public DataTable Data { get; set; } = new DataTable();
    }
}