using Dapper;
using DbNetSuiteCore.Identity.Constants;
using DbNetSuiteCore.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DbNetSuiteCore.Identity.Stores
{
    public class RoleStore : IRoleStore<ApplicationRole>
    {
        private readonly IConfiguration? _configuration = null;
        private readonly IDbConnection? _connection = null;
        private IDbConnection Connection => _connection ?? new SqlConnection(_configuration!.GetConnectionString(Constants.IdentityConstants.ConnectionAlias));

        public RoleStore(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public RoleStore(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sql = @"INSERT INTO Roles (Id, Name, NormalizedName) VALUES (@Id, @Name, @NormalizedName);";

            using (var conn = Connection)
            {
                await conn.ExecuteAsync(sql, role);
            }
            return IdentityResult.Success;
        }

        public async Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sql = "SELECT * FROM Roles WHERE NormalizedName = @Name;";

            using (var conn = Connection)
            {
                return await conn.QuerySingleOrDefaultAsync<ApplicationRole>(sql, new { Name = normalizedRoleName });
            }
        }

        // ... and DeleteAsync, FindByIdAsync, UpdateAsync, etc.
        // ... plus Get/Set RoleName, Get/Set NormalizedRoleName, GetRoleId

        public void Dispose()
        {
            // Nothing to dispose
        }

        // --- Other required methods ---
        public Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<string?> GetNormalizedRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken) => Task.FromResult(role.NormalizedName);
        public Task<string> GetRoleIdAsync(ApplicationRole role, CancellationToken cancellationToken) => Task.FromResult(role.Id.ToString());
        public Task<string?> GetRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken) => Task.FromResult(role.Name);
        public Task SetNormalizedRoleNameAsync(ApplicationRole role, string? normalizedName, CancellationToken cancellationToken) { role.NormalizedName = normalizedName; return Task.CompletedTask; }
        public Task SetRoleNameAsync(ApplicationRole role, string? roleName, CancellationToken cancellationToken) { role.Name = roleName; return Task.CompletedTask; }
        public Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
