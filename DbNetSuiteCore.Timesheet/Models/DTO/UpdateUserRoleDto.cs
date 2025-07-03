namespace DbNetSuiteCore.Timesheet.Models.DTO
{
    public class UpdateUserRoleDto
    {
        public string UserId { get; set; } = string.Empty;
        public bool RoleSelected { get; set; } = false;
        public string RoleId { get; set; } = string.Empty;
    }
}