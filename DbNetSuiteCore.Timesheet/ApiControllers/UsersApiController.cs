using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Timesheet.Constants;
using DbNetSuiteCore.Timesheet.Controllers;
using DbNetSuiteCore.Timesheet.Helpers;
using DbNetSuiteCore.Timesheet.Models;
using DbNetSuiteCore.Timesheet.Models.DTO;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;

namespace DbNetSuiteCore.Timesheet.ApiControllers
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
        [Authorize(Roles = Roles.Administrator)]
        [Route("api/user/getuserroles")]
        public ActionResult<List<string>> GetUserRoles([FromQuery] string id)
        {
            string userId = ApiHelper.DeobfuscateKeyValue(id);
            return GetRoleIds(userId);
        }

        [HttpPost]
        [Authorize(Roles = Roles.Administrator)]
        [Route("api/user/updateuserrole")]
        public async Task<ActionResult<List<string>>> UpdateUserRoleAsync([FromBody] UpdateUserRoleDto updateUserRoleDto)
        {
            string userId = ApiHelper.DeobfuscateKeyValue(updateUserRoleDto.UserId);
            string roleId = ApiHelper.DeobfuscateKeyValue(updateUserRoleDto.RoleId);

            using (var connection = DbHelper.GetConnection(TimesheetConstants.ConnectionAlias, Enums.DataSourceType.MSSQL, _configuration))
            {
                connection.Open();
                CommandConfig commandConfig = new CommandConfig();
                if (updateUserRoleDto.RoleSelected)
                {
                    commandConfig.Sql = "insert into AspNetUserRoles (UserId, RoleId) values (@UserId, @RoleId)";
                }
                else
                {
                    commandConfig.Sql = "delete from AspNetUserRoles where UserId = @UserId and RoleId = @RoleId";
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
            using (var connection = DbHelper.GetConnection(TimesheetConstants.ConnectionAlias, Enums.DataSourceType.MSSQL, _configuration))
            {
                connection.Open();
                QueryCommandConfig query = new QueryCommandConfig() { Sql = "select RoleId from AspNetUserRoles where UserId = @UserId" };
                query.Params["UserId"] = userId;
                var results = DbHelper.RunQuery(query, connection);

                return results.Rows.Cast<DataRow>().Select(r => TextHelper.ObfuscateString(JsonSerializer.Serialize(new List<string>() { r[0].ToString() ?? string.Empty }))).ToList();
            }
        }
    }
}
