using System.Text.RegularExpressions;

namespace DbNetTimeCore.Helpers
{
    public static class TextHelper
    {
        static public string GenerateLabel(string label)
        {
            label = Regex.Replace(label, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");
            return Capitalise(label.Replace("_", " ").Replace(".", " "));
        }
        private static string Capitalise(string text)
        {
            return Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(text);
        }
    }
}
