using Dapper;
using DbNetSuiteCore.Timesheet.Constants;
using DbNetSuiteCore.Timesheet.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient; 
using System.Data;

namespace DbNetSuiteCore.Timesheet.Stores
{
    public class UserStore :
        IUserStore<ApplicationUser>,       
        IUserPasswordStore<ApplicationUser>,
        IUserEmailStore<ApplicationUser>,
        IUserRoleStore<ApplicationUser>
    {
        private readonly IConfiguration _configuration;

        public UserStore(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private IDbConnection Connection =>
            new SqlConnection(_configuration.GetConnectionString(TimesheetConstants.ConnectionAlias));

        public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // You must manually create your database tables first!
            string sql = @"
            INSERT INTO Users (Id, UserName, NormalizedUserName, Email, NormalizedEmail, PasswordHash, SecurityStamp, ConcurrencyStamp, EmailConfirmed)
            VALUES (@Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, @PasswordHash, @SecurityStamp, @ConcurrencyStamp, @EmailConfirmed);";

            using (var conn = Connection)
            {
                await conn.ExecuteAsync(sql, user);
            }
            return IdentityResult.Success;
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

        public async Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            // You need to implement this...
            // e.g., DELETE FROM Users WHERE Id = @Id
            throw new NotImplementedException();
        }

        public async Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            // You need to implement this...
            // e.g., UPDATE Users SET ... WHERE Id = @Id
            throw new NotImplementedException();
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
            string sql = @"Insert into Roles(UserId, RoleId) select Users.Id, Roles.Id from Users join Roles Where Users.Id = @UserId and Roles.Name = @Name";

            using (var conn = Connection)
            {
                await conn.ExecuteAsync(sql, new { UserId = user.Id, Name = roleName });
            }
        }
        public async Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            string sql = @"DELETE from Roles Where UserId = @UserId and RoleId in (select Id from Roles where Name = @Name;";

            using (var conn = Connection)
            {
               await conn.ExecuteAsync(sql, new { UserId = user.Id, Name = roleName });
            }
        }
        public async Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sql = "SELECT Roles.Name FROM Users Join User_Roles on Users.Id = User_Roles.UserId join Roles on Roles.Id = User_Roles.RoleId WHERE Users.Id = @Id;";

            using (var conn = Connection)
            {
                return (await conn.QueryAsync<string>(sql, new { Id = user.Id })).ToList();
            }
        }
        public async Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sql = "SELECT * FROM Roles where Roles.UserId = @Id;";

            using (var conn = Connection)
            {
                return (await conn.QueryAsync(sql, new { Id = user.Id })).Any();
            }

        }
        public async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sql = "SELECT * FROM Users Join Roles on Users.Id = Roles.UserId WHERE Role.Name = @Name;";

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






