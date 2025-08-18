using Microsoft.AspNetCore.Html;

namespace DbNetSuiteCore
{
    public static class Resources
    {
        public const string ClientScriptHtml = "<script src=\"js.htmx\"></script>";
        public const string ClientStyleHtml = "<link rel=\"stylesheet\" href=\"css.htmx\" />";
    
        public static HtmlString ClientScript()
        {
            return new HtmlString(ClientScriptHtml);
        }

        public static HtmlString StyleSheet()
        {
            return new HtmlString(ClientStyleHtml);
        }
    }
}
