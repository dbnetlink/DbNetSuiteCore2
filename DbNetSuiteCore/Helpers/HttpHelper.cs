using DbNetSuiteCore.Models;
using DuckDB.NET.Data;

namespace DbNetSuiteCore.Helpers
{
    public static class HttpHelper
    {
        public static async Task<(bool ok, string error)> CheckDuckDbHttpAccessAsync(string url)
        {
            try
            {
                using (var connection = new DuckDBConnection($"DataSource=:memory:"))
                {
                    connection.Open();
                    using var cmd = connection.CreateCommand();
                    // LIMIT 0 / count-only avoids pulling actual data over the wire,
                    // but still forces DuckDB to open the HTTP connection and validate TLS.
                    cmd.CommandText = "SELECT 1 FROM read_json_auto($url) LIMIT 0";
                    cmd.Parameters.Add(new DuckDBParameter("url", url));

                    await using var reader = await cmd.ExecuteReaderAsync();
                    return (true, null);
                }   

            }
            catch (Exception ex)
            {
                // DuckDB surfaces the libcurl/OpenSSL-style message directly in ex.Message
                return (false, ex.Message);
            }
        }
    }
}
