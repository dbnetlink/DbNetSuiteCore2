namespace DbNetSuiteCore.Models
{
    public class ModifiedRow
    {
        public bool Modified { get; set; } = false;
        public List<string> Columns { get; set; } = new List<string>();
    }
}
