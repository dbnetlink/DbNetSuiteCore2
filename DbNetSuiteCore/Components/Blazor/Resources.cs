using Microsoft.AspNetCore.Components;

namespace DbNetSuiteCore.Blazor
{

    public static class Resources
    {
        public static MarkupString ClientScript()
        {
            return new MarkupString(DbNetSuiteCore.Resources.ClientScriptHtml.Replace("js.htmx", "js.htmx?mode=blazor"));
        }

        public static MarkupString StyleSheet()
        {
            return new MarkupString(DbNetSuiteCore.Resources.ClientStyleHtml);
        }
    }
}