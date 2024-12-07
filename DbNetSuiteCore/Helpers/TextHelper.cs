using DbNetSuiteCore.Enums;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace DbNetSuiteCore.Helpers
{
    public static class TextHelper
    {
        static public string GenerateLabel(string label)
        {
            label = label.Split(".").Last();
            label = label.Replace("[",string.Empty).Replace("]", string.Empty);
            label = Regex.Replace(label, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");
            return Capitalise(label.Replace("_", " ").Replace(".", " "));
        }
        private static string Capitalise(string text)
        {
            return Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(text);
        }
        public static string ObfuscateString(string input, IConfiguration configuration)
        {
            //return input;
            var encryptionConfig = GetEncryptionConfig(configuration);

            if (encryptionConfig.IsValid == false)
            {
                return Compress(input);
            }
            return EncryptionHelper.Encrypt(input, encryptionConfig.Key, encryptionConfig.Salt);
        }

        public static string DeobfuscateString(string input, IConfiguration configuration)
        {
            //return input;
            var encryptionConfig = GetEncryptionConfig(configuration);

            if (encryptionConfig.IsValid == false)
            {
                return Decompress(input);
            }
            return EncryptionHelper.Decrypt(input, encryptionConfig.Key, encryptionConfig.Salt);
        }

        public static string DelimitColumn(string columnName, DataSourceType dataSourceType)
        {
            switch (dataSourceType)
            {
                case DataSourceType.Excel:
                case DataSourceType.JSON:
                    return $"[{columnName}]";
            }
            return columnName;
        }

        public static bool IsAlphaNumeric(string text)
        {
            return text.All(c => char.IsLetterOrDigit(c) ||  c == '_');
        }

        public static string Compress(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            using MemoryStream memoryStream = new MemoryStream();
            using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
            {
                gzipStream.Write(bytes, 0, bytes.Length);
            }
            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public static string Decompress(string compressedText)
        {
            byte[] bytes = Convert.FromBase64String(compressedText);
            using MemoryStream memoryStream = new MemoryStream(bytes);
            using GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            using MemoryStream decompressedStream = new MemoryStream();
            gzipStream.CopyTo(decompressedStream);
            return Encoding.UTF8.GetString(decompressedStream.ToArray());
        }

        public static bool IsAbsolutePath(string path)
        {
            return new Regex(@"^[a-zA-C]:\\").IsMatch(path);
        }

        private static EncryptionConfig GetEncryptionConfig(IConfiguration configuration)
        {
            return new EncryptionConfig()
            {
                Key = configuration.ConfigValue(ConfigurationHelper.AppSetting.EncryptionKey),
                Salt = configuration.ConfigValue(ConfigurationHelper.AppSetting.EncryptionSalt)
            };
        }

        internal class EncryptionConfig
        {
            public string Key { get; set; } = string.Empty;
            public string Salt { get; set; } = string.Empty;

            public bool IsValid => !string.IsNullOrEmpty(Key) && !string.IsNullOrEmpty(Salt);
        }
    }
}