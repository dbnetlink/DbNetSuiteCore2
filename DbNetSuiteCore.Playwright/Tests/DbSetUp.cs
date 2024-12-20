using System.Reflection;

namespace DbNetSuiteCore.Playwright.Tests
{
    public class DbSetUp : SqlLiteDbSetup
    {
        protected string DatabaseName = $"testdb_{Guid.NewGuid():N}".ToLower();
        protected string MasterConnectionString = string.Empty;
        protected string ConnectionString = string.Empty;

        protected string LoadScriptFromFile(string scriptPath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"{assembly.GetName().Name}.{scriptPath.Replace('/', '.')}";

            return LoadTextFromResource(resourceName);
        }

        protected string LoadTextFromResource(string resourceName)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException(
                        $"Could not find embedded resource '{resourceName}'. " +
                        $"Make sure the file exists and its Build Action is set to 'Embedded Resource'.");
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }

}