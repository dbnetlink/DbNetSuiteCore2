namespace DbNetTimeCore.Helpers
{
    public static class RequestHelper
    {
        public static string QueryValue(string key, string defaultValue, HttpContext httpContext)
        {
            return httpContext.Request.Query.ContainsKey(key) ? httpContext.Request.Query[key].ToString() : defaultValue;
        }

        public static string FormValue(string key, string defaultValue, HttpContext httpContext)
        {
            return FormValue(key,defaultValue, (FormCollection)httpContext.Request.Form);
        }

        public static string FormValue(string key, string defaultValue, FormCollection form)
        {
            return form.ContainsKey(key) ? form[key].ToString() : defaultValue;
        }
    }
}
