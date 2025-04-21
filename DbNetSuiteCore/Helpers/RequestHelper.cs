using Azure.Core;
using DbNetSuiteCore.Constants;
using DocumentFormat.OpenXml.InkML;
using System.Runtime.CompilerServices;
using System.Text;
using static DbNetSuiteCore.Helpers.ConfigurationHelper;

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

        public static Dictionary<string,string> FormColumnValues(HttpContext httpContext)
        {
            Dictionary<string, string> formValues = new Dictionary<string, string>();
            foreach (string key in httpContext.Request.Form.Keys)
            {
                if (key.StartsWith("_"))
                {
                    formValues[key.Substring(1)] = httpContext.Request.Form[key];
                }
            }

            return formValues;
        }

        public static Dictionary<string, List<string>> GridFormColumnValues(HttpContext httpContext)
        {
            Dictionary<string, List<string>> formValues = new Dictionary<string, List<string>>();
            foreach (string key in httpContext.Request.Form.Keys)
            {
                if (key.StartsWith("_"))
                {
                    formValues[key.Substring(1)] = FormValueList(key,httpContext);
                }
            }

            return formValues;
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

        public static string Diagnostics(HttpContext httpContext, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            var diagnostics = new List<string>();

            // Request Headers
            diagnostics.Add("<b>=== Headers ===</b>");
            foreach (var header in httpContext.Request.Headers.OrderBy(h => h.Key))
            {
                diagnostics.Add($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            // Content Type Details
            diagnostics.Add("<b>=== Content Type Details ===</b>");
            diagnostics.Add($"Content-Type: {httpContext.Request.ContentType}");
            diagnostics.Add($"Content-Length: {httpContext.Request.ContentLength}");

            // Form Data
            diagnostics.Add("<b>=== Form Data ===</b>");
            if (httpContext.Request.HasFormContentType)
            {
                foreach (var form in httpContext.Request.Form.OrderBy(f => f.Key))
                {
                    switch(form.Key)
                    {
                        case "model":
                            diagnostics.Add($"{form.Key}: {TextHelper.DeobfuscateString(form.Value,configuration)}");
                            break;
                        default:
                            diagnostics.Add($"{form.Key}: {string.Join(", ", form.Value)}");
                            break;
                    }
                }
            }

            // Raw Body
            diagnostics.Add("<b>=== Raw Request Body ===</b>");
            using (var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, true, 1024, true))
            {
                try
                {
                    httpContext.Request.Body.Position = 0; // Reset position to read from start
                    var body = reader.ReadToEndAsync().Result;
                    diagnostics.Add(body);
                    httpContext.Request.Body.Position = 0; // Reset for other middleware
                }
                catch (Exception ex)
                {
                    diagnostics.Add($"Error reading body: {ex.Message}");
                }
            }

            // Environment Information
            diagnostics.Add("<b>=== Environment ===</b>");
            diagnostics.Add($"ASP.NET Core Version: {Environment.Version}");
            diagnostics.Add($"Environment Name: {webHostEnvironment.EnvironmentName}");
            diagnostics.Add($"Application Name: {webHostEnvironment.ApplicationName}");
            diagnostics.Add($"Process Name: {System.Diagnostics.Process.GetCurrentProcess().ProcessName}");
            diagnostics.Add($"Host Name: {System.Net.Dns.GetHostName()}");

            // Environment Information
            diagnostics.Add("<b>=== Settings ===</b>");
            foreach(AppSetting appSetting in Enum.GetValues<AppSetting>())
            {
                diagnostics.Add($"{appSetting}: {configuration.ConfigValue(appSetting)}");
            }
            diagnostics.Add("<b>=== License ===</b>");
            diagnostics.Add($"License: {System.Text.Json.JsonSerializer.Serialize(LicenseHelper.ValidateLicense(configuration, httpContext, webHostEnvironment))}");

            return string.Join("</br>", diagnostics);
        }
    }
}
