using DbNetSuiteCore.Helpers;
using System.Text.Json;

namespace DbNetSuiteCore.Identity.Helpers
{
    public static class ApiHelper
    {
        public static string DeobfuscateKeyValue(string obfuscatedkeyValue)
        {
            var userIdList = JsonSerializer.Deserialize<List<string>>(TextHelper.DeobfuscateString(obfuscatedkeyValue)) ?? new List<string>();
            return userIdList.FirstOrDefault() ?? string.Empty;
        }
    }
}