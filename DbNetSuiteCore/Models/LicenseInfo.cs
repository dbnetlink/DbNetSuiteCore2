using DbNetSuiteCore.Enums;
using System.Text.Json.Serialization;

namespace DbNetSuiteCore.Models
{
    public class LicenseInfo
    {
        public string Id { get; set; } = string.Empty;
        public LicenseType Type { get; set; } = LicenseType.Development;
        public string HostName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        [JsonIgnore]
        public bool LocalRequest { get; set; } = false;
        [JsonIgnore]
        public bool Valid => LocalRequest == true || HostName == ServerHostName || HostName == ApplicationName || Type == LicenseType.OEM;
        [JsonIgnore]
        public string ServerHostName => System.Net.Dns.GetHostName();
        [JsonIgnore]
        public string ApplicationName { get; set; } = string.Empty;
        [JsonIgnore]
        public bool ExisingLicense => LicenseIdIsGuid() && string.IsNullOrEmpty(HostName) == false;

        private bool LicenseIdIsGuid() 
        {
            return Guid.TryParse(Id, out var newGuid);
        }
    }
}
