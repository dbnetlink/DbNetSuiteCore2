using DbNetSuiteCore.Enums;
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
        PatternError
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
    }
}
