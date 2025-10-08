namespace DbNetSuiteCore.Helpers
{

    public static class StateHelper
    {

        public static string GetSerialisedModel(HttpContext httpContext, IConfiguration configuration, string name = "model")
        {
            string model = RequestHelper.FormValue(name, string.Empty, httpContext) ?? string.Empty;
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