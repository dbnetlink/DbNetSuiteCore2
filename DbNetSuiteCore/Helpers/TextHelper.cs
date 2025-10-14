using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Services;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace DbNetSuiteCore.Helpers
{
    public static class TextHelper
    {
        static public string GenerateLabel(string label)
        {
            label = label.Split(".").Last();
            label = label.Replace("[", string.Empty).Replace("]", string.Empty);
            label = Regex.Replace(label, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");
            return Capitalise(label.Replace("_", " ").Replace(".", " "));
        }
        private static string Capitalise(string text)
        {
            return Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(text);
        }

        public static string ObfuscateString(ComponentModel componentModel, IConfiguration? configuration = null)
        {
            return ObfuscateString(Newtonsoft.Json.JsonConvert.SerializeObject(componentModel), configuration, componentModel.HttpContext);
        }

        public static string ObfuscateString(SummaryModel summaryModel, IConfiguration? configuration = null)
        {
            if (summaryModel != null)
            {
                return ObfuscateString(Newtonsoft.Json.JsonConvert.SerializeObject(summaryModel), configuration, summaryModel.HttpContext);
            }
            else
            {
                return string.Empty;
            }
        }

        public static string ObfuscateString(string input, IConfiguration? configuration = null, HttpContext? httpContext = null)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            var encryptionConfig = GetEncryptionConfig(configuration);

            if (encryptionConfig.IsValid == false)
            {
                return Compress(input);
            }

            if (httpContext != null && encryptionConfig.UseDataProtection)
            {
                DataProtectionService? dataProtectionService = httpContext.RequestServices.GetService<DataProtectionService>();
                if (dataProtectionService != null)
                {
                    string text = dataProtectionService.Encrypt(input);
                    return text;
                }
            }


            return EncryptionHelper.Encrypt(input, encryptionConfig.Key, encryptionConfig.Salt);
        }

        public static string DeobfuscateString(string input, IConfiguration? configuration = null, HttpContext? httpContext = null)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            var encryptionConfig = GetEncryptionConfig(configuration);

            if (encryptionConfig.IsValid == false)
            {
                return Decompress(input);
            }

            if (httpContext != null && encryptionConfig.UseDataProtection)
            {
                DataProtectionService? dataProtectionService = httpContext.RequestServices.GetService<DataProtectionService>();
                if (dataProtectionService != null)
                {
                    string text = dataProtectionService.Decrypt(input);
                    if (text != null)
                    {
                        return text;
                    }
                }
            }

            return EncryptionHelper.Decrypt(input, encryptionConfig.Key, encryptionConfig.Salt);
        }

        public static T? DeobfuscateKey<T>(string input)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(TextHelper.DeobfuscateString(input));
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
            return text.All(c => char.IsLetterOrDigit(c) || c == '_');
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new Regex(@"^[a-zA-C]:\\").IsMatch(path);
            }
            else
            {
                return path.StartsWith("/");
            }
        }

        internal static EncryptionConfig GetEncryptionConfig(IConfiguration? configuration = null)
        {
            if (configuration == null)
            {
                return new EncryptionConfig();
            }
            else
            {
                EncryptionConfig encryptionConfig = new EncryptionConfig()
                {
                    Key = configuration.ConfigValue(ConfigurationHelper.AppSetting.EncryptionKey),
                    Salt = configuration.ConfigValue(ConfigurationHelper.AppSetting.EncryptionSalt),
                    DataProtectionPurpose = configuration.ConfigValue(ConfigurationHelper.AppSetting.DataProtectionPurpose),
                    UseDataProtection = ConfigurationHelper.UseDataProtection(configuration)
                };

                encryptionConfig.Key = string.IsNullOrEmpty(encryptionConfig.Key) ? Environment.MachineName : encryptionConfig.Key;
                encryptionConfig.Salt = string.IsNullOrEmpty(encryptionConfig.Salt) ? Environment.MachineName : encryptionConfig.Salt;
                encryptionConfig.DataProtectionPurpose = string.IsNullOrEmpty(encryptionConfig.DataProtectionPurpose) ? Environment.MachineName : encryptionConfig.DataProtectionPurpose;
                return encryptionConfig;
            }
        }

        internal class EncryptionConfig
        {
            public string Key { get; set; } = Environment.MachineName;
            public string Salt { get; set; } = Environment.MachineName;
            public string DataProtectionPurpose { get; set; } = Environment.MachineName;
            public bool IsValid => !string.IsNullOrEmpty(Key) && !string.IsNullOrEmpty(Salt);
            public bool UseDataProtection { get; set; } = false;
        }
    }
}