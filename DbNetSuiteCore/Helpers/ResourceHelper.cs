using DbNetSuiteCore.Enums;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace DbNetSuiteCore.Helpers
{
    public enum ResourceNames
    {
        DataFormatError,
        ColumnFilterNoData,
        SearchPlaceholder,
        Page,
        Of,
        Rows,
        NoRecordsFound,
        NoFilesFound,
        QueryLimited,
        Record,
        Next,
        Previous,
        First,
        Last,
        Apply,
        Cancel,
        ApplyChanges,
        CancelChanges,
        Required,
        Updated,
        UnappliedChanges,
        Insert,
        Delete,
        Deleted,
        ConfirmDelete,
        MinValueError,
        MaxValueError,
        RangeValueError,
        Added,
        PrimaryKeyExists,
        MinCharsError,
        MaxCharsError,
        PatternError, 
        MatchAll,
        MatchAtLeastOne,
        Search,
        Clear,
        Select
    }
    public static class ResourceHelper
    {
        static public string GetResourceString(ResourceNames name)
        {
            return GetResourceString(name.ToString());
        }

        static public string GetResourceString(SearchOperator name)
        {
            return GetResourceString(name.ToString());
        }

        public static string GetResourceString(string name)
        {
            var resourceHelper = new ResourceManager("DbNetSuiteCore.Resources.Text.Strings", Assembly.GetExecutingAssembly());
            return resourceHelper.GetString(name, CultureInfo.CurrentCulture) ?? name;
        }

        public static Dictionary<string,string> GetAllResourceStrings(string? culture = null)
        {
            var cultureInfo = CultureInfo.CurrentCulture;
            if (string.IsNullOrEmpty(culture) == false)
            {
                cultureInfo = new CultureInfo(culture);
            }
            var resourceHelper = new ResourceManager("DbNetSuiteCore.Resources.Text.Strings", Assembly.GetExecutingAssembly());
            ResourceSet resourceSet = resourceHelper.GetResourceSet(cultureInfo, true, true) ?? new ResourceSet(string.Empty);

            Dictionary<string, string> resourceStrings = new Dictionary<string, string>();

            foreach (DictionaryEntry entry in resourceSet)
            {
                resourceStrings.Add(entry.Key?.ToString() ?? string.Empty, entry.Value?.ToString() ?? string.Empty);
            }

            return resourceStrings;
        }
    }
}
