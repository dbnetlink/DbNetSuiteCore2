using System;
using System.Data.Common;
using System.Runtime.InteropServices;

namespace DbNetSuiteCore.Repositories
{
    public interface IDataAccess
    {
        void ExecuteQuery(string query);
        // Add other database operations as needed
    }

    public class OleDbAccess : IDataAccess
    {
        private readonly string _connectionString;
        private static bool? _isSupported;

        public OleDbAccess(string connectionString)
        {
            _connectionString = connectionString;
        }

        public static bool IsSupported
        {
            get
            {
                if (!_isSupported.HasValue)
                {
                    _isSupported = false;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        try
                        {
                            // Attempt to load OleDb type
                            var type = Type.GetType("System.Data.OleDb.OleDbConnection, System.Data.OleDb");
                            _isSupported = type != null;
                        }
                        catch
                        {
                            _isSupported = false;
                        }
                    }
                }
                return _isSupported.Value;
            }
        }

        public void ExecuteQuery(string query)
        {
            if (!IsSupported)
            {
                throw new NotSupportedException("OleDb is not supported on this platform.");
            }

            // Use dynamic to avoid compile-time dependency on OleDb
            try
            {
                dynamic connection = Activator.CreateInstance(
                    Type.GetType("System.Data.OleDb.OleDbConnection, System.Data.OleDb"),
                    _connectionString);

                using (connection)
                {
                    connection.Open();
                    dynamic command = Activator.CreateInstance(
                        Type.GetType("System.Data.OleDb.OleDbCommand, System.Data.OleDb"),
                        query, connection);

                    using (command)
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing OleDb query", ex);
            }
        }
    }

    public class AlternativeDataAccess : IDataAccess
    {
        public void ExecuteQuery(string query)
        {
            // Implement alternative data access method
            // This could be using SQLite, PostgreSQL, etc.
            Console.WriteLine($"Executing query using alternative method: {query}");
        }
    }

    public class DataAccessFactory
    {
        public static IDataAccess CreateDataAccess(string connectionString)
        {
            if (OleDbAccess.IsSupported)
            {
                return new OleDbAccess(connectionString);
            }
            return new AlternativeDataAccess();
        }
    }

    // Usage example
    public class Program
    {
        public static void Main()
        {
            try
            {
                string connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=database.accdb;";
                IDataAccess dataAccess = DataAccessFactory.CreateDataAccess(connectionString);

                dataAccess.ExecuteQuery("SELECT * FROM SomeTable");
            }
            catch (NotSupportedException ex)
            {
                Console.WriteLine($"OleDb not supported: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
