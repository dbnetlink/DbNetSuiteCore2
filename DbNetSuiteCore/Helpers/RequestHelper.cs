using Azure.Core;
using DbNetSuiteCore.Constants;
using DbNetSuiteCore.Models;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Text;
using static DbNetSuiteCore.Helpers.ConfigurationHelper;

namespace DbNetSuiteCore.Helpers
{
    public static class RequestHelper
    {
        public static string? QueryValue(string key, string defaultValue, HttpContext httpContext)
        {
            return httpContext.Request.Query.ContainsKey(key) ? httpContext.Request.Query[key].ToString() : defaultValue;
        }

        public static string? FormValue(string key, string? defaultValue, HttpContext httpContext)
        {
            return FormValue(key, defaultValue, httpContext.Request.Form);
        }

        public static string? FormValue(string key, string? defaultValue, IFormCollection form)
        {
            try
            {
                return form.ContainsKey(key) ? form[key].ToString() : defaultValue;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static Dictionary<string, string> FormColumnValues(HttpContext? httpContext, FormModel formModel)
        {
            Dictionary<string, string> formValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (httpContext?.Request?.Form != null)
            {
                foreach (string key in httpContext.Request.Form.Keys)
                {
                    if (key.StartsWith("_"))
                    {
                        string columnName = formModel.LookupColumnName(key.Substring(1));
                        formValues[columnName] = httpContext.Request.Form[key];
                    }
                }
            }

            return formValues;
        }

        public static Dictionary<string, List<string>> GridFormColumnValues(HttpContext httpContext, GridModel gridModel)
        {
            Dictionary<string, List<string>> formValues = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in httpContext.Request.Form.Keys)
            {
                if (key.StartsWith("_") && key.StartsWith("__") == false)
                {
                    string columnName = gridModel.LookupColumnName(key.Substring(1));
                    formValues[columnName] = FormValueList(key, httpContext).Select(f => f.Trim()).ToList();
                }
            }

            return formValues;
        }


        public static List<ModifiedRow> GetModifiedRows(HttpContext httpContext, GridModel gridModel)
        {
            var modifiedRows = JsonConvert.DeserializeObject<List<ModifiedRow>>(FormValue("modifiedrows", string.Empty, httpContext));

            if (modifiedRows == null)
            {
                return new List<ModifiedRow>();
            }

            foreach (var modifiedRow in modifiedRows)
            {
                ConvertColumnNames(modifiedRow, gridModel);
            }

            return modifiedRows;
        }

        public static ModifiedRow GetModified(HttpContext httpContext, FormModel formModel)
        {
            var modifiedRow = JsonConvert.DeserializeObject<ModifiedRow>(FormValue("modifiedform", string.Empty, httpContext));

            if (modifiedRow == null)
            {
                return new ModifiedRow();
            }

            ConvertColumnNames(modifiedRow, formModel);
            return modifiedRow;
        }

        private static void ConvertColumnNames(ModifiedRow modifiedRow, ComponentModel componentModel)
        {
            List<string> columns = new List<string>();
            foreach (var column in modifiedRow.Columns)
            {
                columns.Add(componentModel.LookupColumnName(column.Substring(1)));
            }

            modifiedRow.Columns = columns;
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
            foreach (string key in httpContext.Request.Headers.Keys)
            {
                if (key.ToLower() == HeaderNames.HxTriggerName.ToLower())
                {
                    return httpContext.Request.Headers[key].ToString();
                }
            }
            return string.Empty;
        }

        public static string Diagnostics(HttpContext? httpContext, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            var diagnostics = new List<string>();

            if (httpContext == null)
            {
                diagnostics.Add("No HttpContext available");
                return string.Join("</br>", diagnostics);
            }

            // Request Headers
            diagnostics.Add("<b>=== Headers ===</b>");
            foreach (var header in httpContext.Request.Headers.OrderBy(h => h.Key))
            {
                if (header.Value.Any())
                {
                    diagnostics.Add($"{header.Key}: {string.Join(", ", header.Value.ToString())}");
                }
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
                    switch (form.Key)
                    {
                        case "model":
                            diagnostics.Add($"{form.Key}: {TextHelper.DeobfuscateString(form.Value, configuration, httpContext)}");
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
            foreach (AppSetting appSetting in Enum.GetValues<AppSetting>())
            {
                diagnostics.Add($"{appSetting}: {configuration.ConfigValue(appSetting)}");
            }
            diagnostics.Add("<b>=== License ===</b>");
            // diagnostics.Add($"License: {JsonConvert.SerializeObject(LicenseHelper.ValidateLicense(configuration, httpContext, webHostEnvironment))}");

            return string.Join("</br>", diagnostics);
        }
    }
}
