using Microsoft.AspNetCore.Html;

namespace DbNetSuiteCore
{
    public static class Resources
    {
        public static HtmlString ClientScript()
        {
            return new HtmlString("<script src=\"js.htmx\"></script>");
        }

        public static HtmlString StyleSheet()
        {
            return new HtmlString("<link rel=\"stylesheet\" href=\"css.htmx\" />");
        }
    }
}
