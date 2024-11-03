using DbNetSuiteCore.Constants;
using System.Text;

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
        #if NET8_0
            return [.. httpContext.Request.Form[key]];
        #else
            return httpContext.Request.Form[key].ToList();
        #endif
        }

        public static string TriggerName(HttpContext httpContext)
        {
            foreach(string key in httpContext.Request.Headers.Keys)
            {
                if (key.ToLower() == HeaderNames.HxTriggerName.ToLower()) 
                {
                    return httpContext.Request.Headers[key].ToString();
                }
            }
            return string.Empty;
        }

        public static string Diagnostics(HttpContext httpContext)
        {
            var diagnostics = new StringBuilder();

            // Request Headers
            diagnostics.AppendLine("=== Headers ===");
            foreach (var header in httpContext.Request.Headers.OrderBy(h => h.Key))
            {
                diagnostics.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            // Content Type Details
            diagnostics.AppendLine("\n=== Content Type Details ===");
            diagnostics.AppendLine($"Content-Type: {httpContext.Request.ContentType}");
            diagnostics.AppendLine($"Content-Length: {httpContext.Request.ContentLength}");

            // Form Data
            diagnostics.AppendLine("\n=== Form Data ===");
            if (httpContext.Request.HasFormContentType)
            {
                foreach (var form in httpContext.Request.Form.OrderBy(f => f.Key))
                {
                    diagnostics.AppendLine($"{form.Key}: {string.Join(", ", form.Value)}");
                }
            }

            // Raw Body
            diagnostics.AppendLine("\n=== Raw Request Body ===");
            using (var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, true, 1024, true))
            {
                try
                {
                    httpContext.Request.Body.Position = 0; // Reset position to read from start
                    var body = reader.ReadToEndAsync().Result;
                    diagnostics.AppendLine(body);
                    httpContext.Request.Body.Position = 0; // Reset for other middleware
                }
                catch (Exception ex)
                {
                    diagnostics.AppendLine($"Error reading body: {ex.Message}");
                }
            }

            // Environment Information
            diagnostics.AppendLine("\n=== Environment ===");
            diagnostics.AppendLine($"ASP.NET Core Version: {Environment.Version}");
            diagnostics.AppendLine($"Environment Name: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
            diagnostics.AppendLine($"Process Name: {System.Diagnostics.Process.GetCurrentProcess().ProcessName}");

            return diagnostics.ToString();
        }
    }
}
