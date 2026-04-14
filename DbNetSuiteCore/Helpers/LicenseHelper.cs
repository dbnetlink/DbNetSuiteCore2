using System.Net.Sockets;
using System.Net;
using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
namespace DbNetSuiteCore.Helpers
{
    public static class LicenseHelper
    {
        /*
        public static string GenerateLicenseKey(LicenseInfo licenseInfo)
        {
            var licenseJson = JsonConvert.SerializeObject(licenseInfo);
            return EncryptionHelper.Encrypt(licenseJson, licenseInfo.Id, Microsoft.VisualBasic.Strings.StrReverse(licenseInfo.Id));
        }

        public static LicenseInfo ValidateLicense(IConfiguration configuration, HttpContext context, IWebHostEnvironment webHostEnvironment)
        {
            return new LicenseInfo() { LocalRequest = true };

            string licenseId = configuration.ConfigValue(ConfigurationHelper.AppSetting.LicenseId);
            string licenseKey = configuration.ConfigValue(ConfigurationHelper.AppSetting.LicenseKey);

            LicenseInfo? licenseInfo = null;

            if (IsDevEnvironment(webHostEnvironment) == false)
            {
                if (LicenseHelper.IsLocalRequest(context) == false)
                {
                    if (string.IsNullOrEmpty(licenseId) == false && string.IsNullOrEmpty(licenseKey) == false)
                    {
                        try
                        {
                            var decryptedJson = EncryptionHelper.Decrypt(licenseKey, licenseId, Microsoft.VisualBasic.Strings.StrReverse(licenseId));
                            licenseInfo = JsonConvert.DeserializeObject<LicenseInfo>(decryptedJson);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            if (licenseInfo == null)
            {
                licenseInfo = new LicenseInfo() { Id = licenseId };
            }

            licenseInfo.LocalRequest = LicenseHelper.IsLocalRequest(context);
            licenseInfo.ApplicationName = webHostEnvironment.ApplicationName;
            return licenseInfo;
        }

        private static bool IsDevEnvironment(IWebHostEnvironment webHostEnvironment)
        {
            return webHostEnvironment.EnvironmentName == Environments.Development;
        }

        public static string HostName()
        {
            return Dns.GetHostName();
        }

        public static bool IsContainer(IConfiguration configuration)
        {
            return bool.TryParse(configuration["DOTNET_RUNNING_IN_CONTAINER"], out bool value) && value;
        }
        */

        public static bool IsLocalRequest(HttpContext httpContext)
        {
            var remoteIp = httpContext.Connection.RemoteIpAddress;
            var localIp = httpContext.Connection.LocalIpAddress;

            if (remoteIp == null || localIp == null)
                return false;

            // Check if IPv4 addresses
            if (remoteIp.IsIPv4MappedToIPv6)
            {
                remoteIp = remoteIp.MapToIPv4();
            }
            if (localIp.IsIPv4MappedToIPv6)
            {
                localIp = localIp.MapToIPv4();
            }

            // Check for localhost
            /*
            if (IPAddress.IsLoopback(remoteIp) ||
                string.Equals(httpContext.Request.Host.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            */

            // Check if client IP matches server IP (local network)
            if (remoteIp.Equals(localIp))
            {
                return true;
            }
            /*
            // Check for private network ranges
            byte[] remoteBytes = remoteIp.GetAddressBytes();
            if (remoteIp.AddressFamily == AddressFamily.InterNetwork)
            {
                // IPv4 private ranges
                // 10.0.0.0/8
                if (remoteBytes[0] == 10)
                    return true;
                // 172.16.0.0/12
                if (remoteBytes[0] == 172 && remoteBytes[1] >= 16 && remoteBytes[1] <= 31)
                    return true;
                // 192.168.0.0/16
                if (remoteBytes[0] == 192 && remoteBytes[1] == 168)
                    return true;
            }
            else if (remoteIp.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // IPv6 private ranges (fc00::/7)
                if (remoteBytes[0] >= 0xfc && remoteBytes[0] <= 0xfd)
                    return true;
            }
            */
            return false;
        }
    }
}