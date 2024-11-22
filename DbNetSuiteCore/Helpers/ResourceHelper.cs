using System.Globalization;
using System.Reflection;
using System.Resources;

namespace DbNetSuiteCore.Helpers
{
    public enum ResourceNames
    {
        ColumnFilterDataError,
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
        UnappliedChanges 
    }
    public static class ResourceHelper
    {
        static public string GetResourceString(ResourceNames name)
        {
            var resourceHelper = new ResourceManager("DbNetSuiteCore.Resources.Text.Strings", Assembly.GetExecutingAssembly());
            return resourceHelper.GetString(name.ToString(), CultureInfo.CurrentCulture) ?? name.ToString();
        }
    }
}
