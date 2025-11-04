using Microsoft.AspNetCore.Html;

namespace DbNetSuiteCore
{
    public static class Resources
    {
        public static readonly string ClientScriptHtml = $"<script src=\"js{DbNetSuiteCore.Middleware.DbNetSuiteCore.Extension}\"></script>";
        public static readonly string ClientStyleHtml = $"<link rel=\"stylesheet\" href=\"css{DbNetSuiteCore.Middleware.DbNetSuiteCore.Extension}\" />";
    
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
