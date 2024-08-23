using System.ComponentModel;

namespace DbNetSuiteCore.Helpers
{
    public static class EnumHelper
    {
        public static List<KeyValuePair<string, string>> GetEnumOptions(Type? enumType)
        {
            var options = new List<KeyValuePair<string, string>>();

            if (enumType != null)
            {
                foreach (Enum i in Enum.GetValues(enumType).Cast<Enum>())
                {
                    var description = GetEnumDescription(i);
                    options.Add(new KeyValuePair<string, string>(Convert.ToInt32(i).ToString(), description));
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
    }
}
