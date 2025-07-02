using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Middleware;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Timesheet.Constants;
using DocumentFormat.OpenXml.Office2010.Excel;
using System.Data;
using System.Text.Json;

namespace DbNetSuiteCore.Timesheet.Helpers
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