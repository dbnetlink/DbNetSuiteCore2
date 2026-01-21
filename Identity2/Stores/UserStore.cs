using Dapper;
using DbNetSuiteCore.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DbNetSuiteCore.Identity.Stores
{
    public class UserStore :
        IUserStore<ApplicationUser>,       
        IUserPasswordStore<ApplicationUser>,
        IUserEmailStore<ApplicationUser>,
        IUserRoleStore<ApplicationUser>
    {
        private readonly IConfiguration? _configuration = null;
        private readonly IDbConnection? _connection = null;
        private IDbConnection Connection => _connection ?? new SqlConnection(_configuration!.GetConnectionString(Constants.IdentityConstants.ConnectionAlias));
        private readonly IRoleStore<ApplicationRole> _roleStore;

        public UserStore(IConfiguration configuration, IRoleStore<ApplicationRole> roleStore)
        {
            _configuration = configuration;
            _roleStore = roleStore;
        }

        public UserStore(IDbConnection connection, IRoleStore<ApplicationRole> roleStore)
        {
            _connection = connection;
            _roleStore = roleStore;
        }

        public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            user.Id = Guid.NewGuid();

            // You must manually create your database tables first!
            string sql = @"
            INSERT INTO Users (Id, UserName, NormalizedUserName, Email, NormalizedEmail, PasswordHash, SecurityStamp, ConcurrencyStamp, EmailConfirmed)
            VALUES (@Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, @PasswordHash, @SecurityStamp, @ConcurrencyStamp, @EmailConfirmed);";


            try
            {
                using (var conn = Connection)
                {
                    await conn.ExecuteAsync(sql, user);
                }
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                return IdentityResult.Failed(new IdentityError { Description = ex.Message });
            }
        }

        public async Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sql = "SELECT * FROM Users WHERE NormalizedUserName = @Name;";

            using (var conn = Connection)
            {
                return await conn.QuerySingleOrDefaultAsync<ApplicationUser>(sql, new { Name = normalizedUserName });
            }
        }

        // --- Implementation of IUserPasswordStore ---

        public Task SetPasswordHashAsync(ApplicationUser user, string? passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public async Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return await Task.FromResult(user.PasswordHash);
        }

        public async Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return await Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
        }


        public async Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sql = "SELECT * FROM Users WHERE Id = @Id;";

            using (var conn = Connection)
            {
                return await conn.QuerySingleOrDefaultAsync<ApplicationUser>(sql, new { Id = Guid.Parse(userId) });
            }
        }

        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(IdentityResult.Success);
        }

        // ... and methods for IUserEmailStore
        public Task SetEmailAsync(ApplicationUser user, string? email, CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.CompletedTask;
        }

        public Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            // You need to implement this...
            // e.g., SELECT * FROM Users WHERE NormalizedEmail = @Email
            throw new NotImplementedException();
        }

        // ... plus Get/Set NormalizedEmail, Get/Set EmailConfirmed, etc.

        // ... and methods for IUserStore base
        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id.ToString());
        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);
        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) { user.UserName = userName; return Task.CompletedTask; }
        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);
        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) { user.NormalizedUserName = normalizedName; return Task.CompletedTask; }
        public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.Email);
        public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.EmailConfirmed);
        public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedEmail);
        public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public Task SetNormalizedEmailAsync(ApplicationUser user, string? normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;
            return Task.CompletedTask;
        }


        public async Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            ApplicationRole? applicationRole = await _roleStore.FindByNameAsync(roleName, cancellationToken);
            if (applicationRole == null)
            {
                applicationRole = new ApplicationRole() { Id = Guid.NewGuid(), Name = roleName, NormalizedName = roleName.ToUpper() };
                var result = await _roleStore.CreateAsync(applicationRole, cancellationToken);
                if (!result.Succeeded)
                {
                    throw new Exception("Failed to create role.");
                }   
            }
            string sql = @"INSERT INTO User_Roles(UserId, RoleId) VALUES (@UserId, @RoleId)";

            using (var conn = Connection)
            {
                await conn.ExecuteAsync(sql, new { UserId = user.Id, RoleId = applicationRole.Id });
            }
        }
        public async Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            string sql = @"DELETE FROM Roles WHERE UserId = @UserId AND RoleId in (SELECT Id FROM Roles WHERE Name = @RoleName;";

            using (var conn = Connection)
            {
               await conn.ExecuteAsync(sql, new { UserId = user.Id, RoleName = roleName });
            }
        }
        public async Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sql = "SELECT Roles.Name FROM Users Join User_Roles on Users.Id = User_Roles.UserId JOIN Roles on Roles.Id = User_Roles.RoleId WHERE Users.Id = @Id;";

            using (var conn = Connection)
            {
                return (await conn.QueryAsync<string>(sql, new { Id = user.Id })).ToList();
            }
        }
        public async Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sql = "SELECT User_Roles.RoleId FROM User_Roles WHERE User_Roles.RoleId in (SELECT Id FROM Roles WHERE Roles.Name = @RoleName) and User_Roles.UserId = @UserId";

            using (var conn = Connection)
            {
                return (await conn.QueryAsync(sql, new { UserId = user.Id, RoleName = roleName })).Any();
            }

        }
        public async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sql = "SELECT * FROM User_Roles WHERE User_Roles.RoleId in (SELECT Id FROM Roles WHERE Roles.Name = @Name);";

            using (var conn = Connection)
            {
                return (await conn.QueryAsync<ApplicationUser>(sql, new { Name = roleName })).ToList();
            }
        }


        public void Dispose()
        {

        }
    }
}






