using System.ComponentModel;

namespace DbNetSuiteCore.Constants
{
    public class FormatType
    {
        [Description("Formats an email address as a clickable link")]
        public const string Email = "email";
        [Description("Formats a url as a clickable link")]
        public const string Url = "url";
        [Description("Converts a link to an image to an image")]
        public const string Image = "image";
    }
}
