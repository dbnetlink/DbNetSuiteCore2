using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Timesheet.Constants;
using DbNetSuiteCore.Timesheet.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Diagnostics;
using System.Text.Json;

namespace DbNetSuiteCore.Timesheet.Controllers
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
        [Route("api/user/getuserroles")]
        public ActionResult<List<string>> Get([FromQuery] string id)
        {
            List<string> roleIds = new List<string>();
            var userIdList = JsonSerializer.Deserialize<List<string>>(TextHelper.DeobfuscateString(id)) ?? new List<string>();
            string userId = userIdList.FirstOrDefault() ?? string.Empty;

            using (var connection = DbHelper.GetConnection(TimesheetConstants.ConnectionAlias, Enums.DataSourceType.MSSQL, _configuration))
            { 
                connection.Open();
                QueryCommandConfig query = new QueryCommandConfig() { Sql = "select RoleId from AspNetUserRoles where UserId = @UserId" };
                query.Params["UserId"] = userId;
                var results = DbHelper.RunQuery(query, connection);

                roleIds = results.Rows.Cast<DataRow>().Select(r => TextHelper.ObfuscateString(JsonSerializer.Serialize(new List<string>() { r[0].ToString() ?? string.Empty }))).ToList();
            }

            return roleIds;
        }
    }
}
