namespace DbNetSuiteCore.Helpers
{

    public static class StateHelper
    {
        public static string GetSerialisedModel(HttpContext? httpContext, IConfiguration configuration, string name = "model")
        {
            if (httpContext == null)
            {
                return string.Empty;
            }
            string model = RequestHelper.FormValue(name, string.Empty, httpContext) ?? string.Empty;
            if (ConfigurationHelper.UseDistributedServerCache(configuration))
            {
                return CacheHelper.GetRedisModel(model, httpContext);
            }
            else if (ConfigurationHelper.ServerStateManagement(configuration))
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