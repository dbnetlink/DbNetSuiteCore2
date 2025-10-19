using System.ComponentModel;

namespace DbNetSuiteCore.Helpers
{
    public static class EnumHelper
    {
        public static List<KeyValuePair<string, string>> GetEnumOptions(Type? enumType, Type dataType)
        {
            var options = new List<KeyValuePair<string, string>>();

            if (enumType != null)
            {
                bool enumHasDescription = EnumHasDescription(enumType);
                foreach (Enum e in Enum.GetValues(enumType).Cast<Enum>())
                {
                    var description = GetEnumDescription(e);
                    var value = dataType == typeof(String) ? e.ToString() : Convert.ToInt32(e).ToString();
                    options.Add(new KeyValuePair<string, string>(value, description));
                }
            }

            return options.OrderBy(kvp => kvp.Value).ToList();
        }
        public static string GetEnumDescription(Enum value)
        {
            if (value == null) { return ""; }

            DescriptionAttribute? attribute = value.GetType().GetField(value.ToString())?.GetCustomAttributes(typeof(DescriptionAttribute), false).SingleOrDefault() as DescriptionAttribute;
            return attribute == null ? value.ToString() : attribute.Description;
        }

        public static bool EnumHasDescription(Type enumType)
        {
            return Enum.GetValues(enumType).Cast<Enum>().Any(e => GetEnumDescription(e) != e.ToString());
        }
    }
}
