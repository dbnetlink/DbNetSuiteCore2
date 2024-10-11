namespace DbNetSuiteCore.Web.Constants
{
    public class ConnectionStringTemplates
    {
        public static readonly string MySql = "server=localhost;database={0};user=root;password=password1234;";
        public static readonly string MSSQL = "Server=localhost;Database={0};Trusted_Connection=True;TrustServerCertificate=True;";
        public static readonly string PostgreSql = "Host=localhost;Username=postgres;Password=password1234;Database={0};pooling=false;";
    }
}