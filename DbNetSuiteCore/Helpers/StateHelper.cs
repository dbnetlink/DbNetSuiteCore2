using DbNetSuiteCore.Models;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace DbNetSuiteCore.Helpers
{

    public static class StateHelper
    {

        public static string GetSerialisedModel(HttpContext httpContext, IConfiguration configuration)
        {
            string model = RequestHelper.FormValue("model", string.Empty, httpContext) ?? string.Empty;
            if (ConfigurationHelper.ServerStateManagement(configuration))
            {
                return CacheHelper.GetModel(model,httpContext);
            }
            else
            {
                return TextHelper.DeobfuscateString(model, configuration, httpContext);
            }
        }
    }
}