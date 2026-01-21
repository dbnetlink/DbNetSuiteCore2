using Dapper;
using DbNetSuiteCore.Identity.Stores;
using Microsoft.Data.Sqlite;
using System.Data;


namespace DbNetSuiteCore.Identity.Tests;
public abstract class IdentityStoreTestBase : IDisposable
{
    // This is the special connection string for a sharable, in-memory SQLite DB
    private const string ConnectionString = "Data Source=TestDb;Mode=Memory;Cache=Shared";

    // We must keep one connection open for the duration of the test
    // to prevent the in-memory database from being destroyed.
    private readonly IDbConnection _masterConnection;

    protected readonly IDbConnection DbConnection;
    protected readonly UserStore UserStore;
    protected readonly RoleStore RoleStore;

    protected IdentityStoreTestBase()
    {
        // 1. Create and open the "master" connection
        _masterConnection = new SqliteConnection(ConnectionString);
        _masterConnection.Open();

        // 2. Create a new connection for the test to use.
        // It will connect to the *same* in-memory database
        // because of the "Cache=Shared" string.
        DbConnection = new SqliteConnection(ConnectionString);
        DbConnection.Open(); // Open this connection for the stores to use

        // 3. Manually create the database schema
        InitializeDatabase();

        // 4. Instantiate your stores, passing the open connection
        // (Your stores must be designed to accept an IDbConnection)
        RoleStore = new RoleStore(DbConnection);
        UserStore = new UserStore(DbConnection, RoleStore);

        SqlMapper.AddTypeHandler(new MySqlGuidTypeHandler());
        SqlMapper.RemoveTypeMap(typeof(Guid));
        SqlMapper.RemoveTypeMap(typeof(Guid?));
    }

    private void InitializeDatabase()
    {
        List<string> tableCreationCommands = new List<string>();
        tableCreationCommands.Add(@"
            CREATE TABLE Users(
	            Id TEXT PRIMARY KEY,
	            UserName TEXT,
	            NormalizedUserName TEXT UNIQUE,
	            Email TEXT,
	            NormalizedEmail  TEXT UNIQUE,
	            EmailConfirmed INTEGER,
	            PasswordHash TEXT,
	            SecurityStamp TEXT,
	            ConcurrencyStamp TEXT,
	            PhoneNumber TEXT,
	            PhoneNumberConfirmed INTEGER,
	            TwoFactorEnabled INTEGER,
	            LockoutEnd TEST,
	            LockoutEnabled INTEGER,
	            AccessFailedCount INTEGER
            );");

        tableCreationCommands.Add(@"
           CREATE TABLE Roles(
	            Id TEXT PRIMARY KEY,
	            Name TEXT,
	            NormalizedName TEXT,
	            ConcurrencyStamp TEXT
            );");

        tableCreationCommands.Add(@"
           CREATE TABLE User_Roles(
	            UserId TEXT,
	            RoleId TEXT
            );");

        foreach (var command in tableCreationCommands)
        {
            _masterConnection.Execute(command);
        }

    }

    public void Dispose()
    {
        DbConnection?.Dispose();
        _masterConnection?.Dispose();
        GC.SuppressFinalize(this);
    }
}