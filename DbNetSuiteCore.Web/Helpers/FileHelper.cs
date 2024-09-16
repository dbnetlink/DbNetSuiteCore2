namespace DbNetSuiteCore.Web.Helpers
{
    public static class FileHelper
    {
        static public object GetJson(string url, IWebHostEnvironment env)
        {
            var path = Combine(url, env.WebRootPath);
            using (StreamReader r = new StreamReader(path))
            {
                return r.ReadToEnd();
            }
        }

        private static string Combine(string relativePath, string basePath)
        {
            relativePath = relativePath.TrimStart('~', '/', '\\');
            string path = Path.Combine(basePath, relativePath);
            string slash = Path.DirectorySeparatorChar.ToString();
            return path
                .Replace("/", slash)
                .Replace("\\", slash)
                .Replace(slash + slash, slash);
        }
    }
}