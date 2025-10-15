namespace DbNetSuiteCore.Helpers
{

    public static class ConfigurationHelper
    {
        public enum AppSetting
        {
            AllowConnectionString,
            UpdateDisabled,
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
    }
}