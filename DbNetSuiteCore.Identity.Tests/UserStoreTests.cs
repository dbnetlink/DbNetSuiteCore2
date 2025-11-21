using Dapper;
using DbNetSuiteCore.Identity.Models;
using DbNetSuiteCore.Identity.Constants;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DbNetSuiteCore.Identity.Tests;
// Inherit from the new Dapper base class
public class UserStoreTests : IdentityStoreTestBase
{
    private readonly CancellationToken _token = CancellationToken.None;

    [Fact]
    public async Task CreateAsync_Should_Add_User_And_Return_Success()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        var result = await UserStore.CreateAsync(user, _token);

        Assert.True(result.Succeeded);

        var savedUser = await UserStore.FindByNameAsync("TESTUSER", _token);

        Assert.NotNull(savedUser);
        Assert.Equal("testuser", savedUser.UserName);
    }

    [Fact]
    public async Task CreateAsync_Should_Return_Failure_For_Duplicate_UserName()
    {
        var user1 = new ApplicationUser { Id = Guid.NewGuid(), UserName = "testuser", NormalizedUserName = "TESTUSER", ConcurrencyStamp = Guid.NewGuid().ToString() };
        var user2 = new ApplicationUser { Id = Guid.NewGuid(), UserName = "testuser", NormalizedUserName = "TESTUSER", ConcurrencyStamp = Guid.NewGuid().ToString() };

        await UserStore.CreateAsync(user1, _token); // Add the first user

          var result = await UserStore.CreateAsync(user2, _token); // Try to add the second

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Description.Contains("UNIQUE"));
    }

    [Fact]
    public async Task AddToRoleAsync_Should_Add_User_Role_And_Return_Success()
    {
        ApplicationUser? adminUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "AdminUser",
            NormalizedUserName = "ADMINUSER",
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        var result = await UserStore.CreateAsync(adminUser, _token);

        Assert.True(result.Succeeded);

        adminUser = await UserStore.FindByNameAsync("ADMINUSER", _token);

        await UserStore.AddToRoleAsync(adminUser!, Roles.Admin , _token);

        ApplicationRole? role = await RoleStore.FindByNameAsync(Roles.Admin.ToUpper(), _token);

        Assert.True((role?.Name ?? string.Empty) == Roles.Admin);

        var userRoles = await UserStore.GetRolesAsync(adminUser!, _token);

        Assert.True((userRoles.Contains(Roles.Admin)));
    }
}