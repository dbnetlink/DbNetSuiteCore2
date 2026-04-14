using DbNetSuiteCore.Models;
using DbNetSuiteCore.Plugins.Interfaces;
using Humanizer;
using System.Collections;
using System.Text.Json.Serialization;

namespace DbNetSuiteCore.Web.Models
{
    public class FloodWarningsTransform : IJsonTransformPlugin
    {
        [JsonPropertyName("@context")]
        public string context { get; set; }
        public MetaData meta { get; set; }
        public List<FloodWarningItem> items { get; set; }

        public IEnumerable Transform(GridModel gridModel)
        {
            return items.Select(i => new
            {
                Description = i.description,
                Area = i.eaAreaName,
                Polygon = i.floodArea.polygon,
                County = i.floodArea.county,
                Message = i.message,
                RaisedAt = i.timeRaised.Humanize(),
                Severity = i.severity,  
                SeverityLevel = i.severityLevel
            });
        }

    }

    public class MetaData
    {
        public string publisher { get; set; }
        public string licence { get; set; }
        public string documentation { get; set; }
        public string version { get; set; }
        public string comment { get; set; }
        public List<string> hasFormat { get; set; }
    }

    public class FloodWarningItem
    {
        [JsonPropertyName("@id")]
        public string id { get; set; }
        public string description { get; set; }
        public string eaAreaName { get; set; }
        public string eaRegionName { get; set; }
        public FloodArea floodArea { get; set; }
        public string floodAreaID { get; set; }
        public bool isTidal { get; set; }
        public string message { get; set; }
        public string severity { get; set; }
        public int severityLevel { get; set; }
        public DateTime timeMessageChanged { get; set; }
        public DateTime timeRaised { get; set; }
        public DateTime timeSeverityChanged { get; set; }
    }

    public class FloodArea
    {
        [JsonPropertyName("@id")]
        public string id { get; set; }
        public string county { get; set; }
        public string notation { get; set; }
        public string polygon { get; set; }
        public string riverOrSea { get; set; }
    }
}
