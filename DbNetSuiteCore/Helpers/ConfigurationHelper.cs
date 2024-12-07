namespace DbNetSuiteCore.Helpers
{

    public static class ConfigurationHelper
    {
        public enum AppSetting
        {
            EncryptionKey,
            EncryptionSalt,
            AllowConnectionString,
            UpdateDisabled
        }

        public static string ConfigValue(this IConfiguration configuration, AppSetting setting)
        {
            return configuration[$"DbNetSuiteCore:{setting}"] ?? string.Empty;
        }
    }
}