using DbNetSuiteCore.Enums;
using System.Text;
using System.Text.RegularExpressions;

namespace DbNetSuiteCore.Helpers
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
        public static string ObfuscateString(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ 0xAA); // XOR with 0xAA
            }
            return Convert.ToBase64String(bytes);
        }

        public static string DeobfuscateString(string input)
        {
            byte[] bytes = Convert.FromBase64String(input);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ 0xAA); // XOR with 0xAA again to reverse
            }
            return Encoding.UTF8.GetString(bytes);
        }

        public static string DelimitColumn(string columnName, DataSourceType dataSourceType)
        {
            if (dataSourceType == DataSourceType.Excel)
            {
                return $"[{columnName}]";
            }
            return columnName;
        }

        public static bool IsAlphaNumeric(string text)
        {
            return text.All(c => char.IsLetterOrDigit(c) ||  c == '_');
        }
    }
}
