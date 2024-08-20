using Azure.Core;

namespace DbNetSuiteCore.Helpers
{
    public static class RequestHelper
    {
        public static string QueryValue(string key, string defaultValue, HttpContext httpContext)
        {
            return httpContext.Request.Query.ContainsKey(key) ? httpContext.Request.Query[key].ToString() : defaultValue;
        }

        public static string FormValue(string key, string defaultValue, HttpContext httpContext)
        {
            return FormValue(key,defaultValue, httpContext.Request.Form);
        }

        public static string FormValue(string key, string defaultValue, IFormCollection form)
        {
            try
            {
                return form.ContainsKey(key) ? form[key].ToString() : defaultValue;
            }
            catch {
                return string.Empty;
            }
        }

        public static List<string> FormValueList(string key, HttpContext httpContext)
        {
            return [.. httpContext.Request.Form[key]];
        }
    }
}
