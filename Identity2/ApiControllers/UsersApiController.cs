using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Identity.Constants;
using DbNetSuiteCore.Identity.Controllers;
using DbNetSuiteCore.Identity.Helpers;
using DbNetSuiteCore.Identity.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.Common;
using System.Text.Json;

namespace DbNetSuiteCore.Identity.ApiControllers
{
    [ApiController] 
    public class UsersApiController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public UsersApiController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        [Route("api/user/getuserroles")]
        public ActionResult<List<string>> GetUserRoles([FromQuery] string id)
        {
            string userId = ApiHelper.DeobfuscateKeyValue(id);
            return GetRoleIds(userId);
        }

        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        [Route("api/user/updateuserrole")]
        public async Task<ActionResult<List<string>>> UpdateUserRoleAsync([FromBody] UpdateUserRoleDto updateUserRoleDto)
        {
            string userId = ApiHelper.DeobfuscateKeyValue(updateUserRoleDto.UserId);
            string roleId = ApiHelper.DeobfuscateKeyValue(updateUserRoleDto.RoleId);

            using (var connection = DbHelper.GetConnection(IdentityConstants.ConnectionAlias, Enums.DataSourceType.MSSQL, _configuration))
            {
                connection.Open();
                CommandConfig commandConfig = new CommandConfig();
                if (updateUserRoleDto.RoleSelected)
                {
                    commandConfig.Sql = "insert into DbNetTime_UserRoles (UserId, RoleId) values (@UserId, @RoleId)";
                }
                else
                {
                    commandConfig.Sql = "delete from DbNetTime_UserRoles where UserId = @UserId and RoleId = @RoleId";
                }
                commandConfig.Params["UserId"] = userId;
                commandConfig.Params["RoleId"] = roleId;

                IDbCommand command = DbHelper.ConfigureCommand(commandConfig, connection);
                await ((DbCommand)command).ExecuteScalarAsync();
            }

            return GetRoleIds(userId); 
        }

        private List<string> GetRoleIds(string userId)
        {
            using (var connection = DbHelper.GetConnection(IdentityConstants.ConnectionAlias, Enums.DataSourceType.MSSQL, _configuration))
            {
                connection.Open();
                QueryCommandConfig query = new QueryCommandConfig() { Sql = "select RoleId from DbNetTime_UserRoles where UserId = @UserId" };
                query.Params["UserId"] = userId;
                var results = DbHelper.RunQuery(query, connection);

                return results.Rows.Cast<DataRow>().Select(r => TextHelper.ObfuscateString(JsonSerializer.Serialize(new List<string>() { r[0].ToString() ?? string.Empty }))).ToList();
            }
        }
    }
}
