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
            StateManagement
        }

        public static string ConfigValue(this IConfiguration configuration, AppSetting setting)
        {
            return configuration[$"DbNetSuiteCore:{setting}"] ?? string.Empty;
        }

        public static bool ServerStateManagement(this IConfiguration configuration)
        {
            return ConfigValue(configuration, AppSetting.StateManagement).ToLower() == "server";
        }
    }
}