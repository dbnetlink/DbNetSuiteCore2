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
                foreach (Enum i in Enum.GetValues(enumType).Cast<Enum>())
                {
                    var description = GetEnumDescription(i);
                    var value = dataType == typeof(String) ? GetEnumDescription(i) : Convert.ToInt32(i).ToString();
                    options.Add(new KeyValuePair<string, string>(value, description));
                }
            }

            return options;
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
