namespace DbNetSuiteCore.Helpers
{

    public static class ConfigurationHelper
    {
        public enum AppSetting
        {
            EncryptionKey,
            EncryptionSalt,
            AllowConnectionString,
            UpdateDisabled,
            LicenseKey,
            LicenseId,
            Locale,
            DataProtectionPurpose,
            UseDataProtection,
            StateManagement,
            UseDistributedServerCache
        }

        public static string ConfigValue(this IConfiguration configuration, AppSetting setting)
        {
            return configuration[$"DbNetSuiteCore:{setting}"] ?? string.Empty;
        }

        public static bool ServerStateManagement(this IConfiguration configuration)
        {
            return ConfigValue(configuration, AppSetting.StateManagement).ToLower() == "server";
        }

        public static bool UseDistributedServerCache(this IConfiguration configuration)
        {
            return configuration.ConfigValue(ConfigurationHelper.AppSetting.UseDistributedServerCache).ToLower() == "true";
        }

        public static bool UseDataProtection(this IConfiguration configuration)
        {
            return configuration.ConfigValue(ConfigurationHelper.AppSetting.UseDataProtection).ToLower() == "true";
        }
    }
}