namespace DbNetSuiteCore.Timesheet.Data.Models
{
    public class Project_Task
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public required Project Project { get; set; }
    }
}
